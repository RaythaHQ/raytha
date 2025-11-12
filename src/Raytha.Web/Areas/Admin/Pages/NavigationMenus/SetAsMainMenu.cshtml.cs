using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class SetAsMainMenu : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new Application.NavigationMenus.Commands.SetAsMainMenu.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
            SetSuccessMessage("Menu is set as main menu successfully.");
        else
            SetErrorMessage(
                "There was an error setting this as the main menu.",
                response.GetErrors()
            );

        return RedirectToPage(RouteNames.NavigationMenus.Edit, new { id });
    }
}
