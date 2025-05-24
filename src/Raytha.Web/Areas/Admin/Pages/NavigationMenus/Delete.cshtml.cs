using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteNavigationMenu.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Menu has been deleted.");
            return RedirectToPage("/NavigationMenus/Index");
        }
        else
        {
            SetErrorMessage("There was an error deleting this menu", response.GetErrors());
            return RedirectToPage("/NavigationMenus/Edit", new { id });
        }
    }
}
