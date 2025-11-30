using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using MockQueryable;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class DiscardDraftContentItemTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
    }

    [Test]
    public async Task Handler_ShouldThrowNotFoundException_WhenContentItemNotFound()
    {
        var contentItems = new List<ContentItem>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);

        var handler = new DiscardDraftContentItem.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new DiscardDraftContentItem.Command { Id = ShortGuid.NewGuid() };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Raytha.Application.Common.Exceptions.NotFoundException>();
    }

    [Test]
    public async Task Handler_ShouldDiscardDraft_WhenCommandIsValid()
    {
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem 
        { 
            Id = contentItemId, 
            IsDraft = true,
            IsPublished = true,
            _DraftContent = "{ \"title\": \"Draft\" }",
            _PublishedContent = "{ \"title\": \"Published\" }",
            ContentType = new ContentType { DeveloperName = "post" }
        };

        var contentItems = new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);

        var handler = new DiscardDraftContentItem.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new DiscardDraftContentItem.Command { Id = contentItemId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        contentItem.IsDraft.Should().BeFalse();
        
        // Parse JSON strings to objects/elements for resilient comparison
        // or compare simplified strings if we control serialization
        // Here we just check it is not null and matches expected content roughly or identical string if serializer is consistent.
        // The error was space difference.
        
        // Use normalizing:
        // contentItem._DraftContent.Replace(" ", "").Should().Be(contentItem._PublishedContent.Replace(" ", ""));
        // Or better, since we assigned it in Handler: entity.DraftContent = entity.PublishedContent;
        // They should be object reference equal if assigned directly?
        // Handler says: entity.DraftContent = entity.PublishedContent;
        // DraftContent property setter serializes to _DraftContent.
        // PublishedContent getter deserializes _PublishedContent.
        
        // If Handler does: entity.DraftContent = entity.PublishedContent;
        // It takes dynamic object from PublishedContent and assigns to DraftContent.
        // Then DraftContent setter serializes it to _DraftContent.
        // So _DraftContent should equal _PublishedContent IF serialization is deterministic and formatting is same.
        // The test failure suggests input _PublishedContent was specific string "{ ... }" (with spaces)
        // But serialization produced "{"..."}" (no spaces).
        
        // So we should compare the deserialized objects or normalize the string.
        
        contentItem._DraftContent.Replace(" ", "").Should().Be(contentItem._PublishedContent.Replace(" ", ""));
        
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

