using CSharpVitamins;
using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Queries;

public class GetContentItemByIdTests
{
    private Mock<IRaythaDbJsonQueryEngine> _dbMock = null!;
    private Mock<IRaythaDbContext> _contextMock = null!;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock = null!;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbJsonQueryEngine>();
        _contextMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
        var relations = new List<WebTemplateContentItemRelation>().AsQueryable().BuildMockDbSet();
        _contextMock.Setup(x => x.WebTemplateContentItemRelations).Returns(relations.Object);
    }

    [Test]
    public async Task Handler_ShouldReturnContentItem_WhenIdIsValid()
    {
        var contentItemId = Guid.NewGuid();
        var contentItemDto = new ContentItem
        {
            Id = contentItemId,
            ContentType = new ContentType { DeveloperName = "post" },
            Route = new Route { Path = "path/to/item" },
        };

        _dbMock.Setup(x => x.FirstOrDefault(contentItemId)).Returns(contentItemDto);

        var handler = new GetContentItemById.Handler(
            _dbMock.Object,
            _contextMock.Object,
            _contentTypeInRoutePathMock.Object
        );
        var query = new GetContentItemById.Query { Id = contentItemId };

        var result = await handler.Handle(query, CancellationToken.None);

        result.Result.Should().NotBeNull();
        result.Result.Id.Should().Be(contentItemId);
        _contentTypeInRoutePathMock.Verify(x => x.ValidateContentTypeInRoutePathMatchesValue("post"), Times.Once);
    }

    [Test]
    public void Handler_ShouldThrowNotFoundException_WhenIdIsInvalid()
    {
        var contentItemId = Guid.NewGuid();
        _dbMock.Setup(x => x.FirstOrDefault(contentItemId)).Returns((ContentItem)null!);

        var handler = new GetContentItemById.Handler(
            _dbMock.Object,
            _contextMock.Object,
            _contentTypeInRoutePathMock.Object
        );
        var query = new GetContentItemById.Query { Id = contentItemId };

        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        act.Should().ThrowAsync<NotFoundException>();
    }
}
