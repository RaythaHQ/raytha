using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Queries;

public class GetContentItemsTests
{
    private const string PublishedFilter = "IsPublished eq 'true'";

    private readonly ContentType _contentType = new()
    {
        Id = Guid.NewGuid(),
        DeveloperName = "posts",
        ContentTypeFields = [],
    };

    private Mock<IRaythaDbJsonQueryEngine> _engine = null!;
    private Mock<IRaythaDbContext> _db = null!;
    private View _view = null!;

    [SetUp]
    public void Setup()
    {
        _view = new View
        {
            Id = Guid.NewGuid(),
            ContentTypeId = _contentType.Id,
            ContentType = _contentType,
            DefaultNumberOfItemsPerPage = 25,
            MaxNumberOfItemsPerPage = 100,
        };
        _engine = new Mock<IRaythaDbJsonQueryEngine>();
        _engine
            .Setup(x =>
                x.QueryContentItems(
                    It.IsAny<Guid>(),
                    It.IsAny<string[]>(),
                    It.IsAny<string>(),
                    It.IsAny<string[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    null
                )
            )
            .Returns([]);
        _db = new Mock<IRaythaDbContext>();
        _db.Setup(x => x.ContentTypes)
            .Returns(new[] { _contentType }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.Views).Returns(new[] { _view }.AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.WebTemplateContentItemRelations)
            .Returns(new List<WebTemplateContentItemRelation>().AsQueryable().BuildMockDbSet().Object);
        _db.Setup(x => x.OrganizationSettings)
            .Returns(
                new List<Raytha.Domain.Entities.OrganizationSettings> { new() { ActiveThemeId = Guid.NewGuid() } }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
    }

    [Test]
    public async Task Published_only_lists_by_content_type_add_the_published_filter()
    {
        await Handle(new GetContentItems.Query { ContentType = "posts", PublishedOnly = true });

        VerifyFilters(filters => filters.Contains(PublishedFilter));
    }

    [Test]
    public async Task Published_only_lists_by_view_add_the_published_filter_even_when_the_view_ignores_client_filters()
    {
        _view.IgnoreClientFilterAndSortQueryParams = true;

        await Handle(new GetContentItems.Query { ViewId = _view.Id, PublishedOnly = true });

        VerifyFilters(filters => filters.Contains(PublishedFilter));
    }

    [Test]
    public async Task Other_lists_include_unpublished_items()
    {
        await Handle(new GetContentItems.Query { ContentType = "posts" });
        await Handle(new GetContentItems.Query { ViewId = _view.Id });

        VerifyFilters(filters => !filters.Contains(PublishedFilter), Times.Exactly(2));
    }

    private async Task Handle(GetContentItems.Query query)
    {
        var handler = new GetContentItems.Handler(
            _engine.Object,
            _db.Object,
            new Mock<IContentTypeInRoutePath>().Object
        );
        await handler.Handle(query, CancellationToken.None);
    }

    private void VerifyFilters(Func<string[], bool> expected, Times? times = null)
    {
        _engine.Verify(
            x =>
                x.QueryContentItems(
                    _contentType.Id,
                    It.IsAny<string[]>(),
                    It.IsAny<string>(),
                    It.Is<string[]>(filters => expected(filters)),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    null
                ),
            times ?? Times.Once()
        );
        _engine.Verify(
            x =>
                x.CountContentItems(
                    _contentType.Id,
                    It.IsAny<string[]>(),
                    It.IsAny<string>(),
                    It.Is<string[]>(filters => expected(filters)),
                    null
                ),
            times ?? Times.Once()
        );
    }
}
