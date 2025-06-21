using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Themes.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteTheme.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Theme has been deleted.");
            return RedirectToPage("/Themes/Index");
        }
        else
        {
            SetErrorMessage("There was an error deleting this theme", response.GetErrors());
            return RedirectToPage("/Themes/Edit", new { id });
        }
    }
}
