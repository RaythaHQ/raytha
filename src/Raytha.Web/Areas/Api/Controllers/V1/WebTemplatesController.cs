using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Templates.Web;
using Raytha.Application.Templates.Web.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class WebTemplatesController : BaseController
{
    [HttpGet("", Name = "GetWebTemplates")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>>> GetWebTemplates(
                                           [FromQuery] GetWebTemplatesAsListItems.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<WebTemplateListItemDto>>;
        return response;
    }

    [HttpGet("{webTemplateId}", Name = "GetWebTemplateById")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<WebTemplateDto>>> GetWebTemplateById(
                                        string webTemplateId)
    {
        var input = new GetWebTemplateById.Query { Id = webTemplateId };
        var response = await Mediator.Send(input) as QueryResponseDto<WebTemplateDto>;
        return response;
    }
}
