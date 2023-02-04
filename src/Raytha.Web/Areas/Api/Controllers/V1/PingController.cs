using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Security;
using Raytha.Application.OrganizationSettings.Queries;
using Raytha.Web.Authentication;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
public class PingController : BaseController
{
    [HttpGet(Name = "ping")]
    public async Task<ActionResult<HealthCheckResult>> Get()
    {
        var response = await Mediator.Send(new GetOrganizationSettings.Query());
        return new HealthCheckResult
        {
            Success = response.Success,
            Version = CurrentVersion.Version,
            OrganizationName = response.Result.OrganizationName
        };
    }
}

public class HealthCheckResult
{
    public bool Success { get; set; }
    public string Version { get; set; }
    public string OrganizationName { get; set; }
}