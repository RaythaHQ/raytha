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

public class CreateContentItemTests
{
    private Mock<IRaythaDbContext> _dbMock;
    private CreateContentItem.Validator _validator;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IRaythaDbContext>();
        _validator = new CreateContentItem.Validator(_dbMock.Object);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenContentTypeDeveloperNameIsEmpty()
    {
        var command = new CreateContentItem.Command { ContentTypeDeveloperName = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result
            .Errors.Should()
            .Contain(x => x.ErrorMessage == "ContentTypeDeveloperName is required.");
    }

    [Test]
    public void Validator_ShouldHaveError_WhenTemplateIdIsEmpty()
    {
        var command = new CreateContentItem.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = ShortGuid.Empty,
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage == "TemplateId is required.");
    }

    [Test]
    public void Validator_ShouldHaveError_WhenContentTypeNotFound()
    {
        // Arrange
        var contentTypes = new List<ContentType>().AsQueryable().BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var command = new CreateContentItem.Command
        {
            ContentTypeDeveloperName = "unknown_type",
            TemplateId = ShortGuid.NewGuid(),
        };

        // Act
        Action act = () => _validator.Validate(command);

        // Assert
        act.Should().Throw<Raytha.Application.Common.Exceptions.NotFoundException>();
    }

    [Test]
    public void Validator_ShouldHaveError_WhenTemplateDoesNotApplyToActiveTheme()
    {
        // Arrange
        var contentTypeId = Guid.NewGuid();
        var contentTypes = new List<ContentType>
        {
            new ContentType { DeveloperName = "post", Id = contentTypeId },
        }
            .AsQueryable()
            .BuildMockDbSet();
        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);

        var activeThemeId = Guid.NewGuid();
        var otherThemeId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        var templates = new List<WebTemplate>
        {
            new WebTemplate { Id = templateId, ThemeId = otherThemeId },
        }
            .AsQueryable()
            .BuildMockDbSet();
        _dbMock.Setup(x => x.WebTemplates).Returns(templates.Object);

        var orgSettings = new List<Raytha.Domain.Entities.OrganizationSettings>
        {
            new Raytha.Domain.Entities.OrganizationSettings { ActiveThemeId = activeThemeId },
        }
            .AsQueryable()
            .BuildMockDbSet();
        _dbMock.Setup(x => x.OrganizationSettings).Returns(orgSettings.Object);

        var command = new CreateContentItem.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = (ShortGuid)templateId,
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result
            .Errors.Should()
            .Contain(x =>
                x.ErrorMessage == "This template does not apply to the current active theme."
            );
    }

    [Test]
    public async Task Handler_ShouldCreateContentItem_WhenCommandIsValid()
    {
        // Arrange
        var contentTypeId = Guid.NewGuid();
        var primaryFieldId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        var contentType = new ContentType
        {
            Id = contentTypeId,
            DeveloperName = "post",
            LabelSingular = "Post",
            PrimaryFieldId = primaryFieldId,
            DefaultRouteTemplate = "{ContentTypeDeveloperName}/{PrimaryField}",
            ContentTypeFields = new List<ContentTypeField>
            {
                new ContentTypeField
                {
                    Id = primaryFieldId,
                    DeveloperName = "title",
                    FieldType = BaseFieldType.SingleLineText,
                },
            },
        };

        var contentTypes = new List<ContentType> { contentType }
            .AsQueryable()
            .BuildMockDbSet();
        var contentItems = new List<ContentItem>().AsQueryable().BuildMockDbSet();
        var webTemplateRelations = new List<WebTemplateContentItemRelation>()
            .AsQueryable()
            .BuildMockDbSet();
        var routes = new List<Route>().AsQueryable().BuildMockDbSet();

        _dbMock.Setup(x => x.ContentTypes).Returns(contentTypes.Object);
        _dbMock.Setup(x => x.ContentItems).Returns(contentItems.Object);
        _dbMock.Setup(x => x.WebTemplateContentItemRelations).Returns(webTemplateRelations.Object);
        _dbMock.Setup(x => x.Routes).Returns(routes.Object);

        var handler = new CreateContentItem.Handler(_dbMock.Object);
        var command = new CreateContentItem.Command
        {
            ContentTypeDeveloperName = "post",
            TemplateId = (ShortGuid)templateId,
            SaveAsDraft = false,
            Content = new Dictionary<string, dynamic> { { "title", "My First Post" } },
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();

        // Match the exact path generation from ToUrlSlug
        // "post/my-first-post" was failed likely due to case sensitivity or exact object match issues
        // We will verify the path specifically

        _dbMock.Verify(
            x =>
                x.ContentItems.Add(
                    It.Is<ContentItem>(c =>
                        c.ContentTypeId == contentTypeId
                        && c.IsPublished == true
                        && c.IsDraft == false
                    )
                ),
            Times.Once
        );

        // Separate verification for the route path to debugging if needed
        _dbMock.Verify(
            x =>
                x.ContentItems.Add(
                    It.Is<ContentItem>(c =>
                        c.Route.Path == "post/My-First-Post" // Case sensitive match based on StringExtensionsTests results
                    )
                ),
            Times.Once
        );

        _dbMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
