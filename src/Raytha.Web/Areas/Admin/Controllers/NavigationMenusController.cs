using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Common.Utils;
using Raytha.Application.NavigationMenuItems;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Application.NavigationMenuItems.Queries;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.NavigationMenus;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class NavigationMenusController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/menus", Name = "menusindex")]
    public async Task<IActionResult> Index(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
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

            return new NavigationMenusListItem_ViewModel
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

        var viewModel = new List_ViewModel<NavigationMenusListItem_ViewModel>(
            items,
            allNavigationMenuResponse.Result.TotalCount
        );

        return View(viewModel);
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/create", Name = "menuscreate")]
    public async Task<IActionResult> Create()
    {
        return View(new NavigationMenusCreate_ViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/create", Name = "menuscreate")]
    public async Task<IActionResult> Create(NavigationMenusCreate_ViewModel model)
    {
        var input = new CreateNavigationMenu.Command
        {
            Label = model.Label,
            DeveloperName = model.DeveloperName,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");

            return RedirectToAction("Edit", new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu. See the error below.",
                response.GetErrors()
            );

            return View(model);
        }
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{id}}", Name = "menusedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var input = new GetNavigationMenuById.Query { Id = id };

        var response = await Mediator.Send(input);

        var viewModel = new NavigationMenusEdit_ViewModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            IsMainMenu = response.Result.IsMainMenu,
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{id}}", Name = "menusedit")]
    public async Task<IActionResult> Edit(NavigationMenusEdit_ViewModel model, string id)
    {
        var input = new EditNavigationMenu.Command { Id = id, Label = model.Label };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was edited successfully.");

            return RedirectToAction(nameof(Edit), new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu. See the error below.",
                response.GetErrors()
            );

            return View(model);
        }
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/delete/{{id}}", Name = "menusdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var input = new DeleteNavigationMenu.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Menu has been deleted.");

            return RedirectToAction(nameof(Index));
        }
        else
        {
            SetErrorMessage("There was an error deleting this menu", response.GetErrors());

            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{id}}/revisions", Name = "menusrevisionsindex")]
    public async Task<IActionResult> Revisions(
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetNavigationMenuRevisionsByNavigationMenuId.Query
        {
            NavigationMenuId = id,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(
            nmr => new NavigationMenuRevisionsListItem_ViewModel
            {
                Id = nmr.Id,
                MenuItems = nmr.NavigationMenuItemsJson,
                CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    nmr.CreationTime
                ),
                CreatorUser = nmr.CreatorUser?.FullName ?? "N/A",
            }
        );

        var viewModel = new NavigationMenuRevisionsPagination_ViewModel(
            items,
            response.Result.TotalCount,
            id
        );

        return View(viewModel);
    }

    [HttpPost]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{id}}/revisions/{{revisionId}}",
        Name = "menusrevisionsrevert"
    )]
    public async Task<IActionResult> RevisionRevert(string id, string revisionId)
    {
        var input = new RevertNavigationMenu.Command { Id = revisionId };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Menu has been reverted.");

            return RedirectToAction(nameof(MenuItems), new { navigationMenuId = id });
        }
        else
        {
            SetErrorMessage("There was an error reverting this menu", response.GetErrors());

            return RedirectToAction(nameof(Revisions), new { id });
        }
    }

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/set-as-main-menu/{{id}}", Name = "menussetasmainmenu")]
    public async Task<IActionResult> SetAsMainMenu(string id)
    {
        var input = new SetAsMainMenu.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
            SetSuccessMessage("Menu is set as main menu successfully.");
        else
            SetErrorMessage(
                "There was an error setting this as the main menu.",
                response.GetErrors()
            );

        return RedirectToAction(nameof(Edit), new { id });
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items", Name = "menuitems")]
    public async Task<IActionResult> MenuItems(string navigationMenuId)
    {
        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.BuildTree<NavigationMenuItemsListItem_ViewModel>(
            (nmi, nestedNavigationMenuItems, _) =>
                new NavigationMenuItemsListItem_ViewModel
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

        var viewModel = new NavigationMenuItems_ViewModel
        {
            Items = items,
            NavigationMenuId = navigationMenuId,
        };

        return View(viewModel);
    }

    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/create",
        Name = "menuitemscreate"
    )]
    public async Task<IActionResult> MenuItemsCreate(string navigationMenuId)
    {
        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);

        var viewModel = new NavigationMenuItemsCreate_ViewModel
        {
            NavigationMenuId = navigationMenuId,
            NavigationMenuItems = response.Result,
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/create",
        Name = "menuitemscreate"
    )]
    public async Task<IActionResult> MenuItemsCreate(
        NavigationMenuItemsCreate_ViewModel model,
        string navigationMenuId
    )
    {
        var input = new CreateNavigationMenuItem.Command
        {
            NavigationMenuId = navigationMenuId,
            Label = model.Label,
            Url = model.Url,
            IsDisabled = model.IsDisabled,
            OpenInNewTab = model.OpenInNewTab,
            CssClassName = model.CssClassName,
            ParentNavigationMenuItemId = !string.IsNullOrEmpty(model.ParentNavigationMenuItemId)
                ? model.ParentNavigationMenuItemId
                : (ShortGuid?)null,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");

            return RedirectToAction(
                nameof(MenuItemsEdit),
                new { navigationMenuId, id = response.Result }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu item. See the error below.",
                response.GetErrors()
            );

            return View(model);
        }
    }

    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/edit/{{id}}",
        Name = "menuitemsedit"
    )]
    public async Task<IActionResult> MenuItemsEdit(string navigationMenuId, string id)
    {
        var navigationMenuItemResponse = await Mediator.Send(
            new GetNavigationMenuItemById.Query { Id = id }
        );

        var allNavigationMenuItemsResponse = await Mediator.Send(
            new GetNavigationMenuItemsByNavigationMenuId.Query
            {
                NavigationMenuId = navigationMenuId,
            }
        );

        var navigationMenuItems = allNavigationMenuItemsResponse
            .Result.ExcludeNestedNavigationMenuItems(id)
            .Where(nmi => nmi.Id != id);
        var viewModel = new NavigationMenuItemsEdit_ViewModel
        {
            Id = navigationMenuItemResponse.Result.Id,
            Label = navigationMenuItemResponse.Result.Label,
            Url = navigationMenuItemResponse.Result.Url,
            IsDisabled = navigationMenuItemResponse.Result.IsDisabled,
            OpenInNewTab = navigationMenuItemResponse.Result.OpenInNewTab,
            CssClassName = navigationMenuItemResponse.Result.CssClassName,
            ParentNavigationMenuItemId = navigationMenuItemResponse
                .Result
                .ParentNavigationMenuItemId,
            NavigationMenuId = navigationMenuId,
            NavigationMenuItems = navigationMenuItems,
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/edit/{{id}}",
        Name = "menuitemsedit"
    )]
    public async Task<IActionResult> MenuItemsEdit(
        NavigationMenuItemsEdit_ViewModel model,
        string navigationMenuId,
        string id
    )
    {
        var input = new EditNavigationMenuItem.Command
        {
            Id = id,
            NavigationMenuId = navigationMenuId,
            Label = model.Label,
            Url = model.Url,
            IsDisabled = model.IsDisabled,
            OpenInNewTab = model.OpenInNewTab,
            CssClassName = model.CssClassName,
            ParentNavigationMenuItemId = !string.IsNullOrEmpty(model.ParentNavigationMenuItemId)
                ? model.ParentNavigationMenuItemId
                : (ShortGuid?)null,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
            SetSuccessMessage($"{model.Label} was edited successfully.");
        else
            SetErrorMessage(
                "There was an error attempting to save this menu item. See the error below.",
                response.GetErrors()
            );

        return RedirectToAction(nameof(MenuItemsEdit), new { id });
    }

    [HttpPost]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/delete/{{id}}",
        Name = "menuitemsdelete"
    )]
    public async Task<IActionResult> MenuItemsDelete(string navigationMenuId, string id)
    {
        var input = new DeleteNavigationMenuItem.Command
        {
            NavigationMenuId = navigationMenuId,
            Id = id,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
            SetSuccessMessage("Menu item has been deleted.");
        else
            SetErrorMessage("There was an error deleting this menu item", response.GetErrors());

        return RedirectToAction(nameof(MenuItems), new { navigationMenuId });
    }

    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/reorder",
        Name = "menuitemsreorder"
    )]
    public async Task<IActionResult> MenuItemsReorder(string navigationMenuId)
    {
        var input = new GetNavigationMenuItemsByNavigationMenuId.Query
        {
            NavigationMenuId = navigationMenuId,
        };

        var response = await Mediator.Send(input);

        var navigationMenuItemsByParentNavigationMenuItemId = response
            .Result.GroupBy(nmi => nmi.ParentNavigationMenuItemId)
            .Where(groups => groups.Count() > 1)
            .ToDictionary(
                g => g.Key?.ToString() ?? string.Empty,
                g =>
                    g.OrderBy(nmi => nmi.Ordinal)
                        .Select(nmi => new NavigationMenuItemsListItem_ViewModel
                        {
                            Id = nmi.Id,
                            Label = nmi.Label,
                            Url = nmi.Url,
                        })
                        .ToList()
            );

        var navigationMenuItemsForSelect = navigationMenuItemsByParentNavigationMenuItemId
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .Select(kv => response.Result.First(nmi => nmi.Id == kv.Key));

        var viewModel = new NavigationMenuItemsReorder_VieModel
        {
            ParentNavigationMenuId = null,
            NavigationMenuItemsByParentNavigationMenuItemId =
                navigationMenuItemsByParentNavigationMenuItemId,
            NavigationMenuItemsForSelect = navigationMenuItemsForSelect,
            NavigationMenuId = navigationMenuId,
        };

        return View(viewModel);
    }

    [HttpPatch]
    [Route(
        $"{RAYTHA_ROUTE_PREFIX}/menus/edit/{{navigationMenuId}}/menu-items/reorder/{{id}}",
        Name = "menuitemsreorderajax"
    )]
    public async Task<IActionResult> MenuItemsReorderAjax([FromForm] string position, string id)
    {
        var input = new ReorderNavigationMenuItems.Command
        {
            Id = id,
            Ordinal = Convert.ToInt32(position),
        };

        var result = await Mediator.Send(input);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Menus";
    }
}
