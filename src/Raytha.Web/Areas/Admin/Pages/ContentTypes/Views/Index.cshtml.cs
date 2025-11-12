using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
public class Index
    : BaseContentTypeContextPageModel,
        IHasListView<Index.ContentTypeViewListItemViewModel>
{
    public ListViewModel<ContentTypeViewListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Label {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView.ContentType.LabelPlural,
                RouteName = RouteNames.ContentItems.Index,
                RouteValues = new Dictionary<string, string>
                {
                    { "contentTypeDeveloperName", CurrentView.ContentType.DeveloperName },
                },
                IsActive = false,
                Icon = SidebarIcons.ContentItems,
            },
            new BreadcrumbNode
            {
                Label = "Views",
                RouteName = RouteNames.ContentTypes.Views.Index,
                IsActive = true,
            }
        );

        var favoriteViewsForAdmin = await Mediator.Send(
            new GetFavoriteViewsForAdmin.Query
            {
                UserId = CurrentUser.UserId.Value,
                ContentTypeId = CurrentView.ContentTypeId,
            }
        );

        var input = new GetViews.Query
        {
            ContentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new ContentTypeViewListItemViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.LastModificationTime
                ),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            IsPublished = p.IsPublished.YesOrNo(),
            RoutePath = p.RoutePath,
            IsFavoriteForAdmin = favoriteViewsForAdmin.Result.Items.Any(c => c.Id == p.Id),
            IsHomePage = CurrentOrganization.HomePageId == p.Id,
        });

        ListView = new ListViewModel<ContentTypeViewListItemViewModel>(
            items,
            response.Result.TotalCount
        );

        return Page();
    }

    public record ContentTypeViewListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Last modified at")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by")]
        public string LastModifierUser { get; init; }

        [Display(Name = "Is published")]
        public string IsPublished { get; init; }

        [Display(Name = "Route path")]
        public string RoutePath { get; init; }

        //helpers
        public bool IsHomePage { get; set; }
        public bool IsFavoriteForAdmin { get; init; }
    }
}
