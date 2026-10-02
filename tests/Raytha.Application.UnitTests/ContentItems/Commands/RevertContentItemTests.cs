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

public class RevertContentItemTests
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
    public async Task Handler_ShouldThrowNotFoundException_WhenContentItemRevisionNotFound()
    {
        var revisions = new List<ContentItemRevision>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentItemRevisions).Returns(revisions.Object);

        var handler = new RevertContentItem.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new RevertContentItem.Command { Id = (ShortGuid)Guid.NewGuid() };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Raytha.Application.Common.Exceptions.NotFoundException>();
    }

    [Test]
    public async Task Handler_ShouldRevertContentItem_WhenCommandIsValid()
    {
        var revisionId = Guid.NewGuid();
        var contentItemId = Guid.NewGuid();
        
        var revision = new ContentItemRevision
        {
            Id = revisionId,
            ContentItemId = contentItemId,
            PublishedContent = new Dictionary<string, dynamic> { { "title", "Old Title" } }
        };

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            _PublishedContent = "{\"title\": \"Current Title\"}",
            _DraftContent = "{\"title\": \"Draft Title\"}",
            IsDraft = false,
            IsPublished = true,
            ContentType = new ContentType { DeveloperName = "post" }
        };

        var revisions = new List<ContentItemRevision> { revision }.AsQueryable().BuildMockDbSet();
        var contentItems = new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet();

        _dbMock.Setup(x => x.ContentItemRevisions).Returns(revisions.Object);
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);

        var handler = new RevertContentItem.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new RevertContentItem.Command { Id = (ShortGuid)revisionId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        
        // Use manual JSON comparison since string formatting might differ slightly
        var expected = "{ \"title\": \"Old Title\" }";
        // Or if you want to verify property assignment directly
        ((string)contentItem._DraftContent).Should().Be(revision._PublishedContent); 
        
        _dbMock.Verify(x => x.ContentItemRevisions.Add(It.IsAny<ContentItemRevision>()), Times.Once);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Reverting_a_published_item_archives_the_live_version_it_replaces()
    {
        var contentItemId = Guid.NewGuid();
        var revision = new ContentItemRevision
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            _PublishedContent = "{\"title\":\"v1\"}",
        };
        var contentItem = new ContentItem
        {
            Id = contentItemId,
            _PublishedContent = "{\"title\":\"v3\"}",
            _DraftContent = "{\"title\":\"v3\"}",
            IsDraft = false,
            IsPublished = true,
            ContentType = new ContentType { DeveloperName = "post" },
        };

        var revisions = new List<ContentItemRevision> { revision }.AsQueryable().BuildMockDbSet();
        ContentItemRevision? archived = null;
        revisions
            .Setup(x => x.Add(It.IsAny<ContentItemRevision>()))
            .Callback<ContentItemRevision>(added => archived = added);
        _dbMock.Setup(x => x.ContentItemRevisions).Returns(revisions.Object);
        _dbMock.Setup(x => x.ContentItems).Returns(new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet().Object);

        var handler = new RevertContentItem.Handler(_dbMock.Object, _contentTypeInRoutePathMock.Object);
        await handler.Handle(new RevertContentItem.Command { Id = (ShortGuid)revision.Id }, CancellationToken.None);

        archived.Should().NotBeNull();
        archived!._PublishedContent.Should().Contain("v3");
        contentItem._PublishedContent.Should().Contain("v1");
    }
}

