using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus.MenuItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Index : BaseAdminPageModel, ISubActionViewModel
{
    public ListViewModel<NavigationMenuItemsListItemViewModel> ListView { get; set; }
    public string NavigationMenuId { get; set; }
    public bool IsNavigationMenuItem { get; set; }
    public string NavigationMenuItemId { get; set; }

    public async Task<IActionResult> OnGet(string navigationMenuId)
    {
        var menuResponse = await Mediator.Send(new GetNavigationMenuById.Query { Id = navigationMenuId });

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Menus",
                RouteName = RouteNames.NavigationMenus.Index,
                IsActive = false,
                Icon = SidebarIcons.Menus,
            },
            new BreadcrumbNode
            {
                Label = menuResponse.Result.Label,
                RouteName = RouteNames.NavigationMenus.Edit,
                RouteValues = new Dictionary<string, string>
                {
                    { "id", navigationMenuId },
                },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Menu items",
                RouteName = RouteNames.NavigationMenus.MenuItems.Index,
                IsActive = true,
            }
        );

        NavigationMenuId = navigationMenuId;
        IsNavigationMenuItem = false;
        NavigationMenuItemId = string.Empty;

        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.BuildTree<NavigationMenuItemsListItemViewModel>(
            (nmi, nestedNavigationMenuItems, _) =>
                new NavigationMenuItemsListItemViewModel
                {
                    Id = nmi.Id,
                    Label = nmi.Label,
                    Url = nmi.Url,
                    IsDisabled = nmi.IsDisabled,
                    OpenInNewTab = nmi.OpenInNewTab,
                    CssClassName = nmi.CssClassName,
                    MenuItems = nestedNavigationMenuItems,
                }
        );

        ListView = new ListViewModel<NavigationMenuItemsListItemViewModel>(
            items,
            response.Result.Count
        );
        return Page();
    }

    public record NavigationMenuItemsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Url")]
        public string Url { get; init; }

        [Display(Name = "Disabled")]
        public bool IsDisabled { get; init; }

        [Display(Name = "Open in new tab")]
        public bool OpenInNewTab { get; init; }

        [Display(Name = "Css class name")]
        public string CssClassName { get; init; }

        public IEnumerable<NavigationMenuItemsListItemViewModel> MenuItems { get; init; } = null!;
    }
}
