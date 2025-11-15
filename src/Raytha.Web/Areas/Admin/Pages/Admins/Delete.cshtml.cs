using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteAdmin.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Administrator has been deleted.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(RouteNames.Admins.Index);
    }
}
