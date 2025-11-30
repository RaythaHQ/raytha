using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using MockQueryable;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class EditContentItemTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;
    private EditContentItem.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
        _validator = new EditContentItem.Validator(_dbMock.Object, _contentTypeInRoutePathMock.Object);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenContentItemNotFound()
    {
        var contentItems = new List<ContentItem>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);

        var command = new EditContentItem.Command { Id = ShortGuid.NewGuid() };

        Action act = () => _validator.Validate(command);

        act.Should().Throw<Raytha.Application.Common.Exceptions.NotFoundException>();
    }

    [Test]
    public async Task Handler_ShouldEditContentItem_WhenCommandIsValid()
    {
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem
        {
            Id = contentItemId,
            IsDraft = false,
            IsPublished = true,
            _PublishedContent = "{\"title\": \"Old Title\"}",
            _DraftContent = "{\"title\": \"Old Title\"}"
        };

        var contentItems = new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet();
        var revisions = new List<ContentItemRevision>().AsQueryable().BuildMockDbSet();

        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);
        _dbMock.Setup(x => x.ContentItemRevisions).Returns(revisions.Object);

        var handler = new EditContentItem.Handler(_dbMock.Object);
        var command = new EditContentItem.Command
        {
            Id = contentItemId,
            SaveAsDraft = false,
            Content = new Dictionary<string, dynamic> { { "title", "New Title" } }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        ((string)((IDictionary<string, dynamic>)contentItem.PublishedContent)["title"]).Should().Be("New Title");
        _dbMock.Verify(x => x.ContentItemRevisions.Add(It.IsAny<ContentItemRevision>()), Times.Once);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

