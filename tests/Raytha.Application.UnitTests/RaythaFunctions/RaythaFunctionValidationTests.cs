using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionValidationTests
{
    private static IRaythaDbContext Db(
        List<RaythaFunction>? functions = null,
        List<RaythaFunctionRevision>? revisions = null
    )
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.RaythaFunctions).Returns((functions ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.RaythaFunctionRevisions)
            .Returns((revisions ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Routes).Returns(new List<Route>().AsQueryable().BuildMockDbSet().Object);
        return db.Object;
    }

    private static CreateRaythaFunction.Command Create(string triggerType) =>
        new()
        {
            Name = "Hello",
            DeveloperName = "hello",
            TriggerType = triggerType,
            Code = "function get() {}",
        };

    [Test]
    public void Create_names_the_valid_trigger_types_instead_of_failing_on_an_unknown_one()
    {
        var result = new CreateRaythaFunction.Validator(Db()).Validate(Create("nope"));

        var error = result.Errors.Should().ContainSingle(e => e.PropertyName == "TriggerType").Subject;
        error.ErrorMessage.Should().Contain("http_request").And.Contain("content_item_deleted");
    }

    [Test]
    public void Create_reports_a_missing_trigger_type_once()
    {
        var result = new CreateRaythaFunction.Validator(Db()).Validate(Create(string.Empty));

        result.Errors.Where(e => e.PropertyName == "TriggerType").Should().ContainSingle();
    }

    [TestCase("http_request")]
    [TestCase("liquid_template")]
    [TestCase("content_item_created")]
    [TestCase("content_item_updated")]
    [TestCase("content_item_deleted")]
    public void Create_accepts_every_supported_trigger_type(string triggerType) =>
        new CreateRaythaFunction.Validator(Db()).Validate(Create(triggerType)).IsValid.Should().BeTrue();

    [Test]
    public void Edit_rejects_an_unknown_trigger_type()
    {
        var function = new RaythaFunction
        {
            Id = Guid.NewGuid(),
            Name = "Hello",
            DeveloperName = "hello",
            TriggerType = RaythaFunctionTriggerType.HttpRequest,
            Code = "x",
        };
        var command = new EditRaythaFunction.Command
        {
            Id = function.Id,
            Name = "Hello",
            TriggerType = "nope",
            Code = "x",
        };

        var result = new EditRaythaFunction.Validator(Db([function])).Validate(command);

        result.Errors.Should().ContainSingle(e => e.PropertyName == "TriggerType");
    }

    [Test]
    public void Revert_scoped_to_a_function_rejects_another_functions_revision()
    {
        var mine = Guid.NewGuid();
        var revision = new RaythaFunctionRevision { Id = Guid.NewGuid(), RaythaFunctionId = Guid.NewGuid(), Code = "x" };
        var command = new RevertRaythaFunction.Command { Id = revision.Id, RaythaFunctionId = mine };

        var validate = () => new RevertRaythaFunction.Validator(Db(revisions: [revision])).Validate(command);

        validate.Should().Throw<NotFoundException>();
    }

    [Test]
    public void Revert_scoped_to_a_function_accepts_its_own_revision()
    {
        var functionId = Guid.NewGuid();
        var revision = new RaythaFunctionRevision { Id = Guid.NewGuid(), RaythaFunctionId = functionId, Code = "x" };
        var command = new RevertRaythaFunction.Command { Id = revision.Id, RaythaFunctionId = functionId };

        new RevertRaythaFunction.Validator(Db(revisions: [revision])).Validate(command).IsValid.Should().BeTrue();
    }

    [Test]
    public void Revert_without_a_function_still_finds_any_revision()
    {
        var revision = new RaythaFunctionRevision { Id = Guid.NewGuid(), RaythaFunctionId = Guid.NewGuid(), Code = "x" };

        new RevertRaythaFunction.Validator(Db(revisions: [revision]))
            .Validate(new RevertRaythaFunction.Command { Id = revision.Id })
            .IsValid.Should().BeTrue();
    }
}
