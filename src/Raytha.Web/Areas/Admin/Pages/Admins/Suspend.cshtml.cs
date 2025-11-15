using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Suspend : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new SetIsActive.Command { Id = id, IsActive = false };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Account has been suspended.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(RouteNames.Admins.Edit, new { id });
    }
}
