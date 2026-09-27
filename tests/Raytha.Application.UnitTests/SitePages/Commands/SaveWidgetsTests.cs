using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.SitePages.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.SitePages.Commands;

public class SaveWidgetsTests
{
    private Mock<IRaythaDbContext> _dbMock = null!;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
    }

    [Test]
    public async Task Handler_RemovesSectionKey_WhenWidgetsAreEmpty()
    {
        var page = PageWithDraft(("main", 1), ("legacy", 2));

        await Save(page, "legacy");

        page.DraftWidgets.Keys.Should().BeEquivalentTo("main");
        page.IsDraft.Should().BeTrue();
    }

    [Test]
    public async Task Handler_RemovesSectionFromPublishedBaseline_WhenNoDraftExists()
    {
        var page = new SitePage { Id = Guid.NewGuid(), IsPublished = true };
        page.PublishedWidgets = Sections(("main", 1), ("legacy", 1));

        await Save(page, "legacy");

        page.DraftWidgets.Keys.Should().BeEquivalentTo("main");
        page.PublishedWidgets.Keys.Should().BeEquivalentTo("main", "legacy");
        page.IsDraft.Should().BeTrue();
    }

    [Test]
    public async Task Handler_DoesNotPersistEmptySection_WhenSectionWasNeverSaved()
    {
        var page = PageWithDraft(("main", 1));

        await Save(page, "sidebar");

        page.DraftWidgets.Keys.Should().BeEquivalentTo("main");
    }

    [Test]
    public async Task Handler_IsIdempotent_WhenRemovingTheSameSectionTwice()
    {
        var page = PageWithDraft(("main", 1), ("legacy", 1));

        await Save(page, "legacy");
        await Save(page, "legacy");

        page.DraftWidgets.Keys.Should().BeEquivalentTo("main");
        page.DraftWidgets["main"].Should().HaveCount(1);
    }

    [Test]
    public async Task Handler_ReplacesSectionWidgets_WhenWidgetsArePresent()
    {
        var page = PageWithDraft(("main", 3));

        await Save(
            page,
            "main",
            new SaveWidgets.WidgetInput { WidgetType = "hero", SettingsJson = "{}" }
        );

        page.DraftWidgets["main"].Should().ContainSingle().Which.WidgetType.Should().Be("hero");
    }

    private async Task Save(SitePage page, string section, params SaveWidgets.WidgetInput[] widgets)
    {
        var pages = new List<SitePage> { page }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.SitePages).Returns(pages.Object);
        var handler = new SaveWidgets.Handler(_dbMock.Object);

        var result = await handler.Handle(
            new SaveWidgets.Command
            {
                Id = page.Id,
                SectionName = section,
                Widgets = widgets,
            },
            CancellationToken.None
        );

        result.Success.Should().BeTrue();
    }

    private static SitePage PageWithDraft(params (string Name, int Count)[] sections)
    {
        var page = new SitePage
        {
            Id = Guid.NewGuid(),
            IsPublished = true,
            IsDraft = true,
        };
        page.DraftWidgets = Sections(sections);
        return page;
    }

    private static Dictionary<string, List<SitePageWidget>> Sections(
        params (string Name, int Count)[] sections
    ) =>
        sections.ToDictionary(
            s => s.Name,
            s =>
                Enumerable
                    .Range(0, s.Count)
                    .Select(row => new SitePageWidget
                    {
                        Id = Guid.NewGuid(),
                        WidgetType = "wysiwyg",
                        Row = row,
                    })
                    .ToList()
        );
}
