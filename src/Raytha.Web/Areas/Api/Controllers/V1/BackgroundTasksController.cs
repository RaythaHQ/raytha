using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.BackgroundTasks;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

/// <summary>
/// Any admin API key can read a task. Import, export, and theme copy jobs are not all
/// gated by one system permission.
/// </summary>
[Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
public class BackgroundTasksController : BaseController
{
    [HttpGet("{id}", Name = "GetBackgroundTaskById")]
    public async Task<ActionResult<IQueryResponseDto<BackgroundTaskDto>>> GetBackgroundTaskById(
        string id
    )
    {
        var response =
            await Mediator.Send(new GetBackgroundTaskById.Query { Id = id })
            as QueryResponseDto<BackgroundTaskDto>;
        return response;
    }
}
