using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Commands;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[AllowAnonymous]
public class ExportAsJson : BaseAdminPageModel
{
    public async Task<IActionResult> OnGet(string developerName)
    {
        var input = new ExportTheme.Command { DeveloperName = developerName };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            return new JsonResult(
                response.Result,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        else
        {
            return StatusCode(StatusCodes.Status400BadRequest, response.Error);
        }
    }
}
