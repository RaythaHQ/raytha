using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;
using System.Threading.Tasks;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Queries;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class WebTemplatesController : BaseController
{
    [HttpGet("", Name = "GetWebTemplates")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>>> GetWebTemplates(
                                           [FromQuery] GetWebTemplatesAsListItems.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<WebTemplateListItemDto>>;
        return response;
    }

    [HttpGet("{webTemplateId}", Name = "GetWebTemplateById")]
    public async Task<ActionResult<IQueryResponseDto<WebTemplateDto>>> GetWebTemplateById(
                                        string webTemplateId)
    {
        var input = new GetWebTemplateById.Query { Id = webTemplateId };
        var response = await Mediator.Send(input) as QueryResponseDto<WebTemplateDto>;
        return response;
    }
}
