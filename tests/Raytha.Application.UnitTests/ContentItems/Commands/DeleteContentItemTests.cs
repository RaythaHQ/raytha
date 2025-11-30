using CSharpVitamins;
using FluentAssertions;
using MockQueryable.Moq;
using MockQueryable;
using Moq;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.UnitTests.ContentItems.Commands;

public class DeleteContentItemTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private Mock<IRaythaDbJsonQueryEngine> _jsonDbMock;
    private Mock<IContentTypeInRoutePath> _contentTypeInRoutePathMock;
    private DeleteContentItem.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _jsonDbMock = new Mock<IRaythaDbJsonQueryEngine>();
        _contentTypeInRoutePathMock = new Mock<IContentTypeInRoutePath>();
        _validator = new DeleteContentItem.Validator(_dbMock.Object);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenTryingToDeleteHomePage()
    {
        var contentItemId = Guid.NewGuid();
        var orgSettings = new List<Raytha.Domain.Entities.OrganizationSettings>
        {
            new Raytha.Domain.Entities.OrganizationSettings { HomePageId = contentItemId }
        }.AsQueryable().BuildMockDbSet();

        _dbMock.Setup(x => x.OrganizationSettings).Returns(orgSettings.Object);

        var command = new DeleteContentItem.Command { Id = contentItemId };
        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("You cannot delete the home page"));
    }

    [Test]
    public async Task Handler_ShouldDeleteContentItem_WhenCommandIsValid()
    {
        var contentItemId = Guid.NewGuid();
        var contentTypeId = Guid.NewGuid();
        var primaryFieldId = Guid.NewGuid();
        
        var contentItemDto = new ContentItem
        {
            Id = contentItemId,
            ContentTypeId = contentTypeId,
            PublishedContent = new Dictionary<string, dynamic> { { "title", "Test Title" } }
        };

        var contentType = new ContentType
        {
            Id = contentTypeId,
            DeveloperName = "post",
            PrimaryFieldId = primaryFieldId,
            ContentTypeFields = new List<ContentTypeField>
            {
                new ContentTypeField 
                { 
                    Id = primaryFieldId, 
                    DeveloperName = "title",
                    FieldType = BaseFieldType.SingleLineText
                }
            }
        };

        var contentItem = new ContentItem
        {
            Id = contentItemId,
            ContentTypeId = contentTypeId,
            _PublishedContent = "{\"title\": \"Test Title\"}",
            Route = new Route { Path = "post/test-title" }
        };

        _jsonDbMock.Setup(x => x.FirstOrDefault(contentItemId)).Returns(contentItemDto);

        var contentTypes = new List<ContentType> { contentType }.AsQueryable().BuildMockDbSet();
        var contentItems = new List<ContentItem> { contentItem }.AsQueryable().BuildMockDbSet();
        var routes = new List<Route> { contentItem.Route }.AsQueryable().BuildMockDbSet();
        var deletedContentItems = new List<DeletedContentItem>().AsQueryable().BuildMockDbSet();
        var webTemplateRelations = new List<WebTemplateContentItemRelation>().AsQueryable().BuildMockDbSet();

        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);
        _dbMock.Setup(x => x.Routes).Returns(routes.Object);
        _dbMock.Setup(x => x.DeletedContentItems).Returns(deletedContentItems.Object);
        _dbMock.Setup(x => x.WebTemplateContentItemRelations).Returns(webTemplateRelations.Object);

        var handler = new DeleteContentItem.Handler(_dbMock.Object, _jsonDbMock.Object, _contentTypeInRoutePathMock.Object);
        var command = new DeleteContentItem.Command { Id = contentItemId };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        _dbMock.Verify(x => x.ContentItems.Remove(contentItem), Times.Once);
        _dbMock.Verify(x => x.DeletedContentItems.Add(It.IsAny<DeletedContentItem>()), Times.Once);
        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

