using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Restore : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new SetIsActive.Command { Id = id, IsActive = true });
        if (response.Success)
        {
            SetSuccessMessage($"Account has been restored.");
        }
        else
        {
            SetErrorMessage(response.Error);
        }

        return RedirectToPage("/Users/Edit", new { id });
    }
}
