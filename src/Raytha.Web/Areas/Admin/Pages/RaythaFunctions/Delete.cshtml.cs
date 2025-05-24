using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteRaythaFunction.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Function has been deleted.");
            return RedirectToPage("/RaythaFunctions/Index");
        }
        else
        {
            SetErrorMessage("There was an error deleting this function", response.GetErrors());
            return RedirectToPage("/RaythaFunctions/Edit", new { id });
        }
    }
}
