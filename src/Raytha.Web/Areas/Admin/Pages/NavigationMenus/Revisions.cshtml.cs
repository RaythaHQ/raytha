using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Revisions : BaseAdminPageModel, ISubActionViewModel
{
    public NavigationMenuRevisionsPaginationViewModel ListView { get; set; }
    public string NavigationMenuId { get; set; }
    public bool IsNavigationMenuItem { get; set; }
    public string NavigationMenuItemId { get; set; }

    public async Task<IActionResult> OnGet(
        string id,
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
                IsActive = false,
                Icon = SidebarIcons.Menus
            },
            new BreadcrumbNode
            {
                Label = "Revisions",
                RouteName = RouteNames.NavigationMenus.Revisions,
                IsActive = true
            }
        );

        NavigationMenuId = id;
        IsNavigationMenuItem = false;
        NavigationMenuItemId = string.Empty;
        var input = new GetNavigationMenuRevisionsByNavigationMenuId.Query
        {
            NavigationMenuId = id,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(nmr => new NavigationMenuRevisionsListItemViewModel
        {
            Id = nmr.Id,
            MenuItems = nmr.NavigationMenuItemsJson,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                nmr.CreationTime
            ),
            CreatorUser = nmr.CreatorUser?.FullName ?? "N/A",
        });

        ListView = new NavigationMenuRevisionsPaginationViewModel(
            items,
            response.Result.TotalCount,
            id
        );

        var (orderByPropertyName, orderByDirection) = orderBy.SplitIntoColumnAndSortOrder();
        ListView.OrderByPropertyName = orderByPropertyName;
        ListView.OrderByDirection = orderByDirection;
        ListView.PageNumber = pageNumber;
        ListView.PageSize = pageSize;

        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string id, string revisionId)
    {
        var input = new RevertNavigationMenu.Command { Id = revisionId };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Menu has been reverted.");
            return RedirectToPage(RouteNames.NavigationMenus.Edit, new { id = response.Result });
        }
        else
        {
            SetErrorMessage("There was an error reverting this menu", response.GetErrors());
            return RedirectToPage(RouteNames.NavigationMenus.Revisions, new { id });
        }
    }

    public record NavigationMenuRevisionsPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<NavigationMenuRevisionsListItemViewModel> Items { get; }
        public string NavigationMenuId { get; set; }

        public NavigationMenuRevisionsPaginationViewModel(
            IEnumerable<NavigationMenuRevisionsListItemViewModel> items,
            int totalCount,
            string navigationMenuId
        )
            : base(totalCount)
        {
            Items = items;
            NavigationMenuId = navigationMenuId;
        }
    }

    public record NavigationMenuRevisionsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Created at")]
        public string CreationTime { get; init; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; init; }

        [Display(Name = "Menu items")]
        public string MenuItems { get; init; }
    }
}
