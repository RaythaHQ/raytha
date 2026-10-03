using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.RaythaFunctions;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionRoutePathTests
{
    private const string Http = "http_request";

    [TestCase("llms.txt", "llms.txt")]
    [TestCase("/llms.txt", "llms.txt")]
    [TestCase("  /feeds/posts.xml/ ", "feeds/posts.xml")]
    [TestCase(null, "")]
    [TestCase("/", "")]
    public void Normalize_strips_whitespace_and_outer_slashes(string? input, string expected) =>
        RaythaFunctionRoutePath.Normalize(input).Should().Be(expected);

    [TestCase("llms.txt")]
    [TestCase("feeds/posts.xml")]
    [TestCase("api-v2/items_list")]
    [TestCase("Robots.TXT")]
    [TestCase("accounts")]
    [TestCase("docs.v1/index")]
    [TestCase("docs/v1.2/install")]
    [TestCase(".well-known/security.txt")]
    [TestCase(".WELL-KNOWN/acme-challenge/token")]
    public void Accepts_a_well_formed_path(string path) =>
        RaythaFunctionRoutePath.Problem(path).Should().BeNull();

    [TestCase("hello world", "only letters")]
    [TestCase("a?b=c", "only letters")]
    [TestCase("llms.txt\r\nSet-Cookie:x", "only letters")]
    [TestCase("a//b", "empty segment")]
    [TestCase("a/../b", "'..'")]
    [TestCase("a..b", "'..'")]
    [TestCase(".env", "'..'")]
    [TestCase("docs/.hidden", "'..'")]
    [TestCase("docs/.well-known/x", "'..'")]
    public void Rejects_a_malformed_path(string path, string reason) =>
        RaythaFunctionRoutePath.Problem(path).Should().Contain(reason);

    [TestCase("raytha")]
    [TestCase("raytha/functions/execute/x")]
    [TestCase("RAYTHA/api")]
    [TestCase("account/login")]
    [TestCase("api/items")]
    [TestCase("_static-files/a.png")]
    [TestCase("healthz")]
    [TestCase("favicon.ico")]
    [TestCase("raytha_default_2026/site.css")]
    public void Rejects_a_path_under_a_reserved_root(string path) =>
        RaythaFunctionRoutePath.Problem(path).Should().Contain("reserves");

    [Test]
    public void Rejects_a_path_longer_than_200_characters() =>
        RaythaFunctionRoutePath.Problem(new string('a', 201)).Should().Contain("200");

    [Test]
    public void Create_rejects_a_path_another_route_already_uses_regardless_of_case()
    {
        var db = Db(routes: [new Route { Id = Guid.NewGuid(), Path = "LLMS.txt", SitePageId = Guid.NewGuid() }]);

        var result = new CreateRaythaFunction.Validator(db).Validate(Create("llms.txt", Http));

        result.Errors.Should().ContainSingle(e => e.PropertyName == "RoutePath")
            .Which.ErrorMessage.Should().Contain("already in use");
    }

    [Test]
    public void Create_rejects_a_reserved_path()
    {
        var result = new CreateRaythaFunction.Validator(Db()).Validate(Create("/raytha/x", Http));

        result.Errors.Should().ContainSingle(e => e.PropertyName == "RoutePath");
    }

    [Test]
    public void Create_rejects_a_path_on_a_function_that_is_not_an_http_trigger()
    {
        var result = new CreateRaythaFunction.Validator(Db()).Validate(Create("llms.txt", "liquid_template"));

        result.Errors.Should().ContainSingle(e => e.PropertyName == "RoutePath")
            .Which.ErrorMessage.Should().Contain("Only HTTP request functions");
    }

    [Test]
    public void Create_accepts_a_blank_path_for_any_trigger()
    {
        var result = new CreateRaythaFunction.Validator(Db()).Validate(Create("  ", "liquid_template"));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Edit_accepts_the_path_the_function_already_owns()
    {
        var function = Function(path: "llms.txt");
        var db = Db([function], [function.Route!]);

        var result = new EditRaythaFunction.Validator(db).Validate(Edit(function, "/LLMS.TXT"));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Edit_rejects_a_path_another_function_owns()
    {
        var function = Function(path: null);
        var other = Function(path: "llms.txt");
        var db = Db([function, other], [other.Route!]);

        var result = new EditRaythaFunction.Validator(db).Validate(Edit(function, "llms.txt"));

        result.Errors.Should().ContainSingle(e => e.PropertyName == "RoutePath");
    }

    [Test]
    public void Apply_creates_a_route_owned_by_the_function()
    {
        var function = Function(path: null);

        RaythaFunctionRoutePath.Apply(Db(), function, "llms.txt");

        function.Route.Should().NotBeNull();
        function.Route!.Path.Should().Be("llms.txt");
        function.Route.RaythaFunctionId.Should().Be(function.Id);
    }

    [Test]
    public void Apply_moves_an_existing_route()
    {
        var function = Function(path: "old.txt");
        var route = function.Route;

        RaythaFunctionRoutePath.Apply(Db(), function, "llms.txt");

        function.Route.Should().BeSameAs(route);
        route!.Path.Should().Be("llms.txt");
    }

    [Test]
    public void Apply_with_an_empty_path_deletes_the_route()
    {
        var function = Function(path: "llms.txt");
        var route = function.Route!;
        var routes = new List<Route> { route }.AsQueryable().BuildMockDbSet();
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.Routes).Returns(routes.Object);

        RaythaFunctionRoutePath.Apply(db.Object, function, string.Empty);

        routes.Verify(x => x.Remove(route), Times.Once);
        function.Route.Should().BeNull();
        function.RouteId.Should().BeNull();
    }

    [Test]
    public async Task Edit_clears_the_path_when_the_trigger_moves_off_http()
    {
        var function = Function(path: "llms.txt");
        var route = function.Route!;
        var routes = new List<Route> { route }.AsQueryable().BuildMockDbSet();
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.RaythaFunctions).Returns(new List<RaythaFunction> { function }.AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Routes).Returns(routes.Object);

        await new EditRaythaFunction.Handler(db.Object).Handle(
            Edit(function, "llms.txt") with { TriggerType = "liquid_template" },
            CancellationToken.None
        );

        routes.Verify(x => x.Remove(route), Times.Once);
        function.RouteId.Should().BeNull();
    }

    [Test]
    public async Task Delete_removes_the_function_route()
    {
        var function = Function(path: "llms.txt");
        var route = function.Route!;
        var routes = new List<Route> { route }.AsQueryable().BuildMockDbSet();
        var functions = new List<RaythaFunction> { function }.AsQueryable().BuildMockDbSet();
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.RaythaFunctions).Returns(functions.Object);
        db.Setup(x => x.Routes).Returns(routes.Object);

        await new DeleteRaythaFunction.Handler(db.Object).Handle(
            new DeleteRaythaFunction.Command { Id = function.Id },
            CancellationToken.None
        );

        routes.Verify(x => x.Remove(route), Times.Once);
        functions.Verify(x => x.Remove(function), Times.Once);
    }

    private static CreateRaythaFunction.Command Create(string routePath, string triggerType) =>
        new()
        {
            Name = "Llms",
            DeveloperName = "llms",
            TriggerType = triggerType,
            Code = "function get() {}",
            RoutePath = routePath,
        };

    private static EditRaythaFunction.Command Edit(RaythaFunction function, string routePath) =>
        new()
        {
            Id = function.Id,
            Name = function.Name,
            TriggerType = Http,
            Code = function.Code,
            IsActive = true,
            RoutePath = routePath,
        };

    private static RaythaFunction Function(string? path)
    {
        var function = new RaythaFunction
        {
            Id = Guid.NewGuid(),
            Name = "Llms",
            DeveloperName = "llms_" + Guid.NewGuid().ToString("N")[..6],
            TriggerType = RaythaFunctionTriggerType.HttpRequest,
            Code = "function get() {}",
            IsActive = true,
        };
        if (path != null)
        {
            function.Route = new Route { Id = Guid.NewGuid(), Path = path, RaythaFunctionId = function.Id };
            function.RouteId = function.Route.Id;
        }
        return function;
    }

    private static IRaythaDbContext Db(List<RaythaFunction>? functions = null, List<Route>? routes = null)
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.RaythaFunctions)
            .Returns((functions ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Routes).Returns((routes ?? []).AsQueryable().BuildMockDbSet().Object);
        return db.Object;
    }
}
