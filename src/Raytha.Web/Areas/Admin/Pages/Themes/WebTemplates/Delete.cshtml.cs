using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WebTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string themeId, string id)
    {
        var input = new DeleteWebTemplate.Command { Id = id };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage("Web-template has been deleted.");
            return RedirectToPage("/Themes/WebTemplates/Index", new { themeId });
        }
        else
        {
            SetErrorMessage("There was an error deleting this web-template", response.GetErrors());
            return RedirectToPage("/Themes/WebTemplates/Edit", new { themeId, id });
        }
    }
}
