using Microsoft.AspNetCore.Mvc;
using Raytha.Application.OrganizationSettings.Queries;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Area("Api")]
public class MainController : BaseController
{
    [HttpGet]
    [Route(RAYTHA_ROUTE_PREFIX + "/ping")]
    public async Task<IActionResult> Ping()
    {
        //make a database call
        var response = await Mediator.Send(new GetOrganizationSettings.Query());
        if (response.Success)
        {
            return Ok(Json(new { success = true }));
        }
        else
        {
            return BadRequest(Json(new { success = false }));
        }
    }
}