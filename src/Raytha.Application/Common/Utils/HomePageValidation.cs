using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Utils;

public static class HomePageValidation
{
    public const string CannotUnpublishMessage =
        "You cannot unpublish the home page. Change the home page first and then try again.";
    public const string CannotSetUnpublishedMessage =
        "You cannot set an unpublished page as the home page. Publish the page first and then try again.";

    public static bool IsHomePage(IRaythaDbContext db, Guid id, string routeType) =>
        db.OrganizationSettings.Any(settings =>
            settings.HomePageId == id && settings.HomePageType == routeType
        );

    public static bool IsPublished(IRaythaDbContext db, Guid id, string routeType)
    {
        bool? isPublished = routeType switch
        {
            Route.CONTENT_ITEM_TYPE => db
                .ContentItems.Where(item => item.Id == id)
                .Select(item => (bool?)item.IsPublished)
                .FirstOrDefault(),
            Route.SITE_PAGE_TYPE => db
                .SitePages.Where(page => page.Id == id)
                .Select(page => (bool?)page.IsPublished)
                .FirstOrDefault(),
            Route.VIEW_TYPE => db
                .Views.Where(view => view.Id == id)
                .Select(view => (bool?)view.IsPublished)
                .FirstOrDefault(),
            _ => null,
        };

        return isPublished != false;
    }
}
