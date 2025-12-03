using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Themes.WidgetTemplates;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Application.Themes.WidgetTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION
)]
public class WidgetTemplatesController : BaseController
{
    [HttpGet("", Name = "GetWidgetTemplates")]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>>
    > GetWidgetTemplates(
        string? themeDeveloperName = null,
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetWidgetTemplatesAsListItems.Query
        {
            ThemeDeveloperName = themeDeveloperName,
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var response =
            await Mediator.Send(input)
            as QueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>;
        return response;
    }

    [HttpGet("{widgetTemplateId}", Name = "GetWidgetTemplateById")]
    public async Task<ActionResult<IQueryResponseDto<WidgetTemplateDto>>> GetWidgetTemplateById(
        string widgetTemplateId
    )
    {
        var input = new GetWidgetTemplateById.Query { Id = widgetTemplateId };
        var response = await Mediator.Send(input) as QueryResponseDto<WidgetTemplateDto>;
        return response;
    }

    [HttpGet(
        "theme/{themeDeveloperName}/template/{templateDeveloperName}",
        Name = "GetWidgetTemplateByDeveloperName"
    )]
    public async Task<
        ActionResult<IQueryResponseDto<WidgetTemplateDto>>
    > GetWidgetTemplateByDeveloperName(string themeDeveloperName, string templateDeveloperName)
    {
        var input = new GetWidgetTemplateByDeveloperNames.Query
        {
            ThemeDeveloperName = themeDeveloperName,
            TemplateDeveloperName = templateDeveloperName,
        };
        var response = await Mediator.Send(input) as QueryResponseDto<WidgetTemplateDto>;
        return response;
    }

    [HttpPut(
        "theme/{themeDeveloperName}/template/{templateDeveloperName}",
        Name = "EditWidgetTemplate"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditWidgetTemplate(
        string themeDeveloperName,
        string templateDeveloperName,
        [FromBody] EditWidgetTemplateByDeveloperName.Command request
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
