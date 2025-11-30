using CSharpVitamins;
using FluentAssertions;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentItems;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Queries;

public class GetContentItemByIdTests
{
    private Mock<IRaythaDbJsonQueryEngine> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbJsonQueryEngine>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
    }

    [Test]
    public async Task Handler_ShouldReturnContentItem_WhenIdIsValid()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        var contentItemDto = new ContentItem
        {
            Id = contentItemId,
            ContentType = new ContentType { DeveloperName = "post" },
            Route = new Route { Path = "path/to/item" }
        };

        _dbMock.Setup(x => x.FirstOrDefault(contentItemId)).Returns(contentItemDto);

        var handler = new GetContentItemById.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var query = new GetContentItemById.Query { Id = contentItemId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Result.Should().NotBeNull();
        result.Result.Id.Should().Be(contentItemId);
        _contentTypeInRoutePathMock.Verify(x => x.ValidateContentTypeInRoutePathMatchesValue("post"), Times.Once);
    }

    [Test]
    public void Handler_ShouldThrowNotFoundException_WhenIdIsInvalid()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        _dbMock.Setup(x => x.FirstOrDefault(contentItemId)).Returns((ContentItem)null);

        var handler = new GetContentItemById.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var query = new GetContentItemById.Query { Id = contentItemId };

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<NotFoundException>();
    }
}

