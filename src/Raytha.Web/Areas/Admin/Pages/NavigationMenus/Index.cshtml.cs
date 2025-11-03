using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Index : BaseAdminPageModel
{
    public ListViewModel<NavigationMenusListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Menus",
                RouteName = RouteNames.NavigationMenus.Index,
                IsActive = true,
                Icon = SidebarIcons.Menus
            }
        );

        var input = new GetNavigationMenus.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var allNavigationMenuResponse = await Mediator.Send(input);
        var latestNavigationMenuRevisionsResponse = await Mediator.Send(
            new GetLatestNavigationMenuRevisions.Query()
        );

        var items = allNavigationMenuResponse.Result.Items.Select(nm =>
        {
            var latestRevision = latestNavigationMenuRevisionsResponse.Result.FirstOrDefault(nmr =>
                nmr.NavigationMenuId == nm.Id
            );
            var (lastModificationTime, lastModificationUser) =
                Nullable.Compare(nm.LastModificationTime, latestRevision?.CreationTime) > 0
                    ? (nm.LastModificationTime, nm.LastModifierUser)
                    : (latestRevision?.CreationTime, latestRevision?.CreatorUser);

            return new NavigationMenusListItemViewModel
            {
                Id = nm.Id,
                Label = nm.Label,
                DeveloperName = nm.DeveloperName,
                IsMainMenu = nm.IsMainMenu,
                LastModificationTime =
                    CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                        lastModificationTime
                    ),
                LastModifierUser = lastModificationUser?.FullName ?? "N/A",
            };
        });

        ListView = new ListViewModel<NavigationMenusListItemViewModel>(
            items,
            allNavigationMenuResponse.Result.TotalCount
        );

        var (orderByPropertyName, orderByDirection) = orderBy.SplitIntoColumnAndSortOrder();
        ListView.OrderByPropertyName = orderByPropertyName;
        ListView.OrderByDirection = orderByDirection;
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;
        ListView.Search = search;

        return Page();
    }

    public record NavigationMenusListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Is main menu")]
        public bool IsMainMenu { get; init; }

        [Display(Name = "Last modified at")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by")]
        public string LastModifierUser { get; init; }
    }
}
