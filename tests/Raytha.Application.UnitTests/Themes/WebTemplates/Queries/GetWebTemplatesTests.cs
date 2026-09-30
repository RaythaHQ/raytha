using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.Themes.WebTemplates.Queries;

public class GetWebTemplatesTests
{
    private readonly Guid _themeId = Guid.NewGuid();
    private readonly User _admin = new() { Id = Guid.NewGuid() };
    private readonly User _otherAdmin = new() { Id = Guid.NewGuid() };
    private Mock<IRaythaDbContext> _dbMock = null!;

    [SetUp]
    public void Setup()
    {
        var templates = new List<WebTemplate>
        {
            Template("Echo"),
            Template("Alpha"),
            Template("Delta", [_admin]),
            Template("Charlie", [_otherAdmin]),
            Template("Bravo", [_admin, _otherAdmin]),
            Template("Foxtrot layout", isBaseLayout: true),
            Template("Golf layout", [_admin], isBaseLayout: true),
        };
        _dbMock = new Mock<IRaythaDbContext>();
        _dbMock.Setup(x => x.WebTemplates).Returns(templates.AsQueryable().BuildMockDbSet().Object);
    }

    [Test]
    public async Task Handler_ListsTheAdminsFavoritesFirst_ThenEveryoneElseByLabel()
    {
        var page = await List(new GetWebTemplates.Query { CurrentUserId = _admin.Id });

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Bravo", "Delta", "Golf layout", "Alpha", "Charlie", "Echo", "Foxtrot layout");
        page.Items.Select(t => t.IsFavorite)
            .Should()
            .Equal(true, true, true, false, false, false, false);
        page.TotalCount.Should().Be(7);
    }

    [Test]
    public async Task Handler_IgnoresAnotherAdminsFavorites()
    {
        var page = await List(new GetWebTemplates.Query { CurrentUserId = _otherAdmin.Id });

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Bravo", "Charlie", "Alpha", "Delta", "Echo", "Foxtrot layout", "Golf layout");
    }

    [Test]
    public async Task Handler_OrdersByLabelOnly_WhenNoAdminIsGiven()
    {
        var page = await List(new GetWebTemplates.Query());

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot layout", "Golf layout");
        page.Items.Should().OnlyContain(t => !t.IsFavorite);
    }

    [Test]
    public async Task Handler_PagesAcrossTheFavoritesBoundaryWithoutGapsOrRepeats()
    {
        var labels = new List<string>();
        for (var pageNumber = 1; pageNumber <= 3; pageNumber++)
        {
            var page = await List(
                new GetWebTemplates.Query
                {
                    CurrentUserId = _admin.Id,
                    PageNumber = pageNumber,
                    PageSize = 3,
                }
            );
            page.TotalCount.Should().Be(7);
            labels.AddRange(page.Items.Select(t => t.Label));
        }

        labels
            .Should()
            .Equal("Bravo", "Delta", "Golf layout", "Alpha", "Charlie", "Echo", "Foxtrot layout");
    }

    [Test]
    public async Task Handler_KeepsFavoritesFirst_WithinASearch()
    {
        var page = await List(new GetWebTemplates.Query { CurrentUserId = _admin.Id, Search = "o" });

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Bravo", "Golf layout", "Echo", "Foxtrot layout");
    }

    [Test]
    public async Task Handler_KeepsFavoritesFirst_WithinTheBaseLayoutFilter()
    {
        var page = await List(
            new GetWebTemplates.Query { CurrentUserId = _admin.Id, BaseLayoutsOnly = true }
        );

        page.Items.Select(t => t.Label).Should().Equal("Golf layout", "Foxtrot layout");
    }

    [Test]
    public async Task Handler_AppliesTheRequestedOrderWithinEachGroup()
    {
        var page = await List(
            new GetWebTemplates.Query { CurrentUserId = _admin.Id, OrderBy = "Label desc" }
        );

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Golf layout", "Delta", "Bravo", "Foxtrot layout", "Echo", "Charlie", "Alpha");
    }

    [Test]
    public async Task Handler_FallsBackToLabelOrder_WhenTheRequestedOrderIsUnknown()
    {
        var page = await List(
            new GetWebTemplates.Query { CurrentUserId = _admin.Id, OrderBy = "Nope desc" }
        );

        page.Items.Select(t => t.Label)
            .Should()
            .Equal("Bravo", "Delta", "Golf layout", "Alpha", "Charlie", "Echo", "Foxtrot layout");
    }

    private async Task<Raytha.Application.Common.Models.ListResultDto<WebTemplateDto>> List(
        GetWebTemplates.Query query
    )
    {
        var handler = new GetWebTemplates.Handler(_dbMock.Object);
        var response = await handler.Handle(query with { ThemeId = _themeId }, CancellationToken.None);
        response.Success.Should().BeTrue();
        return response.Result;
    }

    private WebTemplate Template(
        string label,
        User[]? favoritedBy = null,
        bool isBaseLayout = false
    ) =>
        new()
        {
            Id = Guid.NewGuid(),
            ThemeId = _themeId,
            Label = label,
            DeveloperName = label.ToLowerInvariant().Replace(' ', '_'),
            IsBaseLayout = isBaseLayout,
            UserFavorites = (favoritedBy ?? []).ToList(),
        };
}
