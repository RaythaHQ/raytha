using CSharpVitamins;
using FluentAssertions;
using FluentValidation;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Themes;

public class ThemeTemplateUsageTests
{
    private readonly Theme _theme = new() { Id = Guid.NewGuid(), Title = "T", DeveloperName = "t", Description = "" };
    private WebTemplate _detail = null!;
    private WebTemplate _layout = null!;
    private Mock<IRaythaDbContext> _db = null!;

    private readonly List<WebTemplateViewRelation> _viewRelations = [];
    private readonly List<WebTemplateContentItemRelation> _itemRelations = [];
    private readonly List<SitePage> _pages = [];

    [SetUp]
    public void Setup()
    {
        _viewRelations.Clear();
        _itemRelations.Clear();
        _pages.Clear();
        _layout = new WebTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = _theme.Id,
            Label = "Layout",
            DeveloperName = "layout",
            IsBaseLayout = true,
        };
        _detail = new WebTemplate
        {
            Id = Guid.NewGuid(),
            ThemeId = _theme.Id,
            Label = "Detail",
            DeveloperName = "detail",
            ParentTemplateId = _layout.Id,
        };

        _db = new Mock<IRaythaDbContext>();
        _db.Setup(d => d.Themes).Returns(new List<Theme> { _theme }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(d => d.WebTemplates)
            .Returns(new List<WebTemplate> { _layout, _detail }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(d => d.WebTemplateViewRelations).Returns(() => _viewRelations.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(d => d.WebTemplateContentItemRelations).Returns(() => _itemRelations.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(d => d.SitePages).Returns(() => _pages.AsQueryable().BuildMockDbSet().Object);
    }

    private async Task<IReadOnlyList<GetThemeTemplateUsage.TemplateUsage>> Run()
    {
        var handler = new GetThemeTemplateUsage.Handler(_db.Object);
        var result = await handler.Handle(
            new GetThemeTemplateUsage.Query { ThemeId = _theme.Id },
            CancellationToken.None
        );
        return result.Result.Templates;
    }

    [Test]
    public async Task UnboundTemplates_AreNotInUse()
    {
        var usage = await Run();

        var detail = usage.Single(t => t.DeveloperName == "detail");
        detail.InUse.Should().BeFalse();
        detail.ContentItems.Count.Should().Be(0);
        // A base layout is in use as soon as a template inherits from it.
        usage.Single(t => t.DeveloperName == "layout").InUse.Should().BeTrue();
    }

    [Test]
    public async Task ReportsViewsItemsPagesAndChildren_PerTemplate()
    {
        var type = new ContentType { Id = Guid.NewGuid(), DeveloperName = "posts" };
        var view = new View { Id = Guid.NewGuid(), Label = "All", DeveloperName = "all", ContentType = type };
        var item = new ContentItem { Id = Guid.NewGuid(), Route = new Route { Path = "posts/hello" }, ContentType = type };
        _viewRelations.Add(new WebTemplateViewRelation { WebTemplateId = _detail.Id, ViewId = view.Id, View = view });
        _itemRelations.Add(new WebTemplateContentItemRelation { WebTemplateId = _detail.Id, ContentItemId = item.Id, ContentItem = item });
        _pages.Add(new SitePage { Id = Guid.NewGuid(), Title = "About", WebTemplateId = _detail.Id });

        var usage = await Run();
        var detail = usage.Single(t => t.DeveloperName == "detail");
        var layout = usage.Single(t => t.DeveloperName == "layout");

        detail.InUse.Should().BeTrue();
        detail.Views.Should().ContainSingle().Which.Should().Be(
            new GetThemeTemplateUsage.ViewUsage(view.Id, "All", "all", "posts"));
        detail.ContentItems.Count.Should().Be(1);
        detail.ContentItems.Sample.Single().RoutePath.Should().Be("posts/hello");
        detail.SitePages.Should().ContainSingle().Which.Title.Should().Be("About");
        layout.InUse.Should().BeTrue();
        layout.ChildTemplates.Should().ContainSingle().Which.DeveloperName.Should().Be("detail");
        layout.Views.Should().BeEmpty();
    }

    [Test]
    public async Task ContentItems_AreCountedInFullButSampled()
    {
        var type = new ContentType { Id = Guid.NewGuid(), DeveloperName = "posts" };
        for (var i = 0; i < 25; i++)
        {
            var item = new ContentItem { Id = Guid.NewGuid(), Route = new Route { Path = $"posts/p{i:00}" }, ContentType = type };
            _itemRelations.Add(new WebTemplateContentItemRelation { WebTemplateId = _detail.Id, ContentItemId = item.Id, ContentItem = item });
        }

        var detail = (await Run()).Single(t => t.DeveloperName == "detail");

        detail.ContentItems.Count.Should().Be(25);
        detail.ContentItems.Sample.Should().HaveCount(20);
    }

    [Test]
    public void UnknownTheme_IsNotFound()
    {
        var handler = new GetThemeTemplateUsage.Handler(_db.Object);

        FluentActions
            .Awaiting(async () => await handler.Handle(
                new GetThemeTemplateUsage.Query { ThemeId = Guid.NewGuid() }, CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>();
    }

    [Test]
    public void DeleteWebTemplate_IsRefusedWhileASitePageUsesTheTemplate()
    {
        _pages.Add(new SitePage { Id = Guid.NewGuid(), Title = "About", WebTemplateId = _detail.Id });
        _db.Setup(d => d.WebTemplateContentItemRelations).Returns(new List<WebTemplateContentItemRelation>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(d => d.WebTemplateViewRelations).Returns(new List<WebTemplateViewRelation>().AsQueryable().BuildMockDbSet().Object);
        var validator = new DeleteWebTemplate.Validator(_db.Object);

        var result = validator.Validate(new DeleteWebTemplate.Command { Id = (ShortGuid)_detail.Id });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("site pages"));
    }
}
