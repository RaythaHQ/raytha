using CSharpVitamins;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using ContentItemSetAsHomePage = Raytha.Application.ContentItems.Commands.SetAsHomePage;
using DomainOrganizationSettings = Raytha.Domain.Entities.OrganizationSettings;
using SitePageSetAsHomePage = Raytha.Application.SitePages.Commands.SetSitePageAsHomePage;
using ViewSetAsHomePage = Raytha.Application.Views.Commands.SetAsHomePage;

namespace Raytha.Application.UnitTests;

public class HomePagePublicationTests
{
    private const string CannotUnpublishMessage =
        "You cannot unpublish the home page. Change the home page first and then try again.";
    private const string CannotSetUnpublishedMessage =
        "You cannot set an unpublished page as the home page. Publish the page first and then try again.";

    [Test]
    public void Content_item_home_page_cannot_be_unpublished()
    {
        var id = Guid.NewGuid();
        var db = CreateDb(
            organizationSettings: new() { HomePageId = id, HomePageType = Route.CONTENT_ITEM_TYPE }
        );

        var result = Validate(
            typeof(Raytha.Application.ContentItems.Commands.UnpublishContentItem),
            db.Object,
            new Raytha.Application.ContentItems.Commands.UnpublishContentItem.Command
            {
                Id = (ShortGuid)id,
            }
        );

        AssertSummaryFailure(result, CannotUnpublishMessage);
    }

    [Test]
    public void Site_page_home_page_cannot_be_unpublished()
    {
        var id = Guid.NewGuid();
        var db = CreateDb(
            organizationSettings: new() { HomePageId = id, HomePageType = Route.SITE_PAGE_TYPE }
        );

        var result = Validate(
            typeof(Raytha.Application.SitePages.Commands.UnpublishSitePage),
            db.Object,
            new Raytha.Application.SitePages.Commands.UnpublishSitePage.Command
            {
                Id = (ShortGuid)id,
            }
        );

        AssertSummaryFailure(result, CannotUnpublishMessage);
    }

    [Test]
    public void View_home_page_cannot_be_unpublished()
    {
        var id = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var contentTypeId = Guid.NewGuid();
        var view = new View
        {
            Id = id,
            ContentType = new ContentType { Id = contentTypeId },
        };
        var template = new WebTemplate
        {
            Id = templateId,
            ThemeId = Guid.NewGuid(),
            TemplateAccessToModelDefinitions =
            [
                new() { ContentTypeId = contentTypeId },
            ],
        };
        var db = CreateDb(
            organizationSettings: new() { HomePageId = id, HomePageType = Route.VIEW_TYPE },
            views: [view],
            webTemplates: [template]
        );

        var result = Validate(
            typeof(Raytha.Application.Views.Commands.EditPublicSettings),
            db.Object,
            new Raytha.Application.Views.Commands.EditPublicSettings.Command
            {
                Id = (ShortGuid)id,
                TemplateId = (ShortGuid)templateId,
                RoutePath = "home",
                IsPublished = false,
                DefaultNumberOfItemsPerPage = 10,
                MaxNumberOfItemsPerPage = 10,
            }
        );

        AssertSummaryFailure(result, CannotUnpublishMessage);
    }

    [Test]
    public void Unpublished_content_item_cannot_be_set_as_home_page()
    {
        var id = Guid.NewGuid();
        var db = CreateDb(contentItems: [new() { Id = id, IsPublished = false }]);

        var result = Validate(
            typeof(ContentItemSetAsHomePage),
            db.Object,
            new ContentItemSetAsHomePage.Command { Id = (ShortGuid)id }
        );

        AssertSummaryFailure(result, CannotSetUnpublishedMessage);
    }

    [Test]
    public void Unpublished_site_page_cannot_be_set_as_home_page()
    {
        var id = Guid.NewGuid();
        var db = CreateDb(sitePages: [new() { Id = id, IsPublished = false }]);

        var result = Validate(
            typeof(SitePageSetAsHomePage),
            db.Object,
            new SitePageSetAsHomePage.Command { Id = (ShortGuid)id }
        );

        AssertSummaryFailure(result, CannotSetUnpublishedMessage);
    }

    [Test]
    public void Unpublished_view_cannot_be_set_as_home_page()
    {
        var id = Guid.NewGuid();
        var db = CreateDb(views: [new() { Id = id, IsPublished = false }]);

        var result = Validate(
            typeof(ViewSetAsHomePage),
            db.Object,
            new ViewSetAsHomePage.Command { Id = (ShortGuid)id }
        );

        AssertSummaryFailure(result, CannotSetUnpublishedMessage);
    }

    private static ValidationResult Validate(Type commandContainer, IRaythaDbContext db, object command)
    {
        var validatorType = commandContainer.GetNestedType("Validator");
        validatorType.Should().NotBeNull($"{commandContainer.Name} must validate the invariant");

        var validator = Activator.CreateInstance(validatorType!, db);
        validator.Should().BeAssignableTo<IValidator>();

        return ((dynamic)validator!).Validate((dynamic)command);
    }

    private static void AssertSummaryFailure(ValidationResult result, string message)
    {
        result.Errors.Should()
            .ContainSingle(error => error.PropertyName == "__ValidationSummary")
            .Which.ErrorMessage.Should()
            .Be(message);
    }

    private static Mock<IRaythaDbContext> CreateDb(
        DomainOrganizationSettings? organizationSettings = null,
        IReadOnlyCollection<ContentItem>? contentItems = null,
        IReadOnlyCollection<SitePage>? sitePages = null,
        IReadOnlyCollection<View>? views = null,
        IReadOnlyCollection<WebTemplate>? webTemplates = null
    )
    {
        var db = new Mock<IRaythaDbContext>();
        db.Setup(x => x.OrganizationSettings)
            .Returns(
                new List<DomainOrganizationSettings> { organizationSettings ?? new() }
                    .AsQueryable()
                    .BuildMockDbSet()
                    .Object
            );
        db.Setup(x => x.ContentItems)
            .Returns((contentItems ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.SitePages)
            .Returns((sitePages ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Views).Returns((views ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.WebTemplates)
            .Returns((webTemplates ?? []).AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Routes)
            .Returns(new List<Route>().AsQueryable().BuildMockDbSet().Object);
        return db;
    }
}
