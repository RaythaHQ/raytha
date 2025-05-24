using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenuItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus.MenuItems;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string navigationMenuId, string id)
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

        return RedirectToPage("/NavigationMenus/MenuItems/Index", new { navigationMenuId });
    }
}
