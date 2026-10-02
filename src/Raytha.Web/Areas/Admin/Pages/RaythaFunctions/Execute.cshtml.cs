using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Services;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class Execute : BaseAdminPageModel
{
    public async Task<IActionResult> OnGet(string developerName)
    {
        return await ExecuteFunction(developerName);
    }

    public async Task<IActionResult> OnPost(string developerName)
    {
        return await ExecuteFunction(developerName);
    }

    private async Task<IActionResult> ExecuteFunction(string developerName)
    {
        var input = await RaythaFunctionHttp.ReadCommand(Request, developerName);
        if (input is null)
        {
            return RaythaFunctionHttp.MethodNotAllowed(Response);
        }

        var response = await Mediator.Send(input, HttpContext.RequestAborted);
        return RaythaFunctionHttp.ToActionResult(Request, response);
    }
}
