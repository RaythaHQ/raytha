using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION
)]
public class WebTemplatesController : BaseController
{
    [HttpGet("", Name = "GetWebTemplates")]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>>
    > GetWebTemplates(
        string? themeDeveloperName = null,
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetWebTemplatesAsListItems.Query
        {
            ThemeDeveloperName = themeDeveloperName,
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var response =
            await Mediator.Send(input) as QueryResponseDto<ListResultDto<WebTemplateListItemDto>>;
        return response;
    }

    [HttpGet("{webTemplateId}", Name = "GetWebTemplateById")]
    public async Task<ActionResult<IQueryResponseDto<WebTemplateDto>>> GetWebTemplateById(
        string webTemplateId
    )
    {
        var input = new GetWebTemplateById.Query { Id = webTemplateId };
        var response = await Mediator.Send(input) as QueryResponseDto<WebTemplateDto>;
        return response;
    }

    [HttpGet(
        "theme/{themeDeveloperName}/template/{templateDeveloperName}",
        Name = "GetWebTemplateByDeveloperName"
    )]
    public async Task<
        ActionResult<IQueryResponseDto<WebTemplateDto>>
    > GetWebTemplateByDeveloperName(string themeDeveloperName, string templateDeveloperName)
    {
        var input = new GetWebTemplateByDeveloperNames.Query
        {
            ThemeDeveloperName = themeDeveloperName,
            TemplateDeveloperName = templateDeveloperName,
        };
        var response = await Mediator.Send(input) as QueryResponseDto<WebTemplateDto>;
        return response;
    }

    [HttpPost("theme/{themeDeveloperName}", Name = "CreateWebTemplate")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateWebTemplate(
        string themeDeveloperName,
        [FromBody] CreateWebTemplateByDeveloperName.Command request
    )
    {
        var input = request with { ThemeDeveloperName = themeDeveloperName };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetWebTemplateById),
            new { webTemplateId = response.Result },
            response
        );
    }

    [HttpPut(
        "theme/{themeDeveloperName}/template/{templateDeveloperName}",
        Name = "EditWebTemplate"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditWebTemplate(
        string themeDeveloperName,
        string templateDeveloperName,
        [FromBody] EditWebTemplateByDeveloperName.Command request
    )
    {
        var input = request with
        {
            ThemeDeveloperName = themeDeveloperName,
            TemplateDeveloperName = templateDeveloperName,
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}
