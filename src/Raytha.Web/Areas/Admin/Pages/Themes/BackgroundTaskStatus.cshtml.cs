using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class BackgroundTaskStatus : BaseAdminPageModel
{
    public string PathBase { get; set; }

    public async Task<IActionResult> OnGet(string id, bool json = false)
    {
        var response = await Mediator.Send(new GetBackgroundTaskById.Query { Id = id });
        PathBase = CurrentOrganization.PathBase;
        return json ? new JsonResult(response.Result) : Page();
    }
}
