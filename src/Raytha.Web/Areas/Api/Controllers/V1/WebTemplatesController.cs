using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates;
using Raytha.Application.Themes.WebTemplates.Commands;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Public.DbViewEngine;
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

    [HttpGet("{webTemplateId}/render-preview", Name = "RenderWebTemplatePreview")]
    public async Task<IActionResult> RenderPreview(
        string webTemplateId,
        string? contentItemId = null,
        string? viewId = null
    )
    {
        if (!string.IsNullOrEmpty(contentItemId) && !string.IsNullOrEmpty(viewId))
        {
            return TemplateRenderProblem.ToResult(
                new ProblemDetails
                {
                    Type = "https://httpstatuses.io/400",
                    Title = TemplateRenderProblem.Title,
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Provide a content item or a view.",
                    Instance = HttpContext.Request.Path.Value,
                }
            );
        }

        var template = await Mediator.Send(new GetWebTemplateById.Query { Id = webTemplateId });
        object? target = null;
        ContentType_RenderModel? contentType = null;
        if (!string.IsNullOrEmpty(contentItemId))
        {
            var item = await Mediator.Send(new GetContentItemById.Query { Id = contentItemId });
            target = ContentItem_RenderModel.GetProjection(
                item.Result,
                template.Result.DeveloperName,
                previewDraft: !item.Result.IsPublished
            );
            contentType = ContentType_RenderModel.GetProjection(item.Result.ContentType);
        }
        else if (!string.IsNullOrEmpty(viewId))
        {
            (target, contentType) = await RenderViewTargetAsync(viewId);
        }

        try
        {
            var html = WebTemplatePreview.Render(HttpContext, template.Result, target, contentType);
            return Content(html, "text/html");
        }
        catch (TemplateRenderException ex)
        {
            return TemplateRenderProblem.ToResult(
                TemplateRenderProblem.Create(ex, HttpContext.Request.Path.Value)
            );
        }
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

    [HttpPost("validate", Name = "ValidateWebTemplate")]
    public async Task<ActionResult<IQueryResponseDto<ValidateWebTemplateSyntax.TemplateSyntaxResult>>> Validate(
        [FromBody] ValidateWebTemplateSyntax.Query request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Result.IsValid)
        {
            return BadRequest(
                new
                {
                    success = false,
                    error = response.Result.Error,
                    line = response.Result.Line,
                    column = response.Result.Column,
                }
            );
        }
        return Ok(response);
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

    [HttpDelete(
        "theme/{themeDeveloperName}/template/{templateDeveloperName}",
        Name = "DeleteWebTemplate"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteWebTemplate(
        string themeDeveloperName,
        string templateDeveloperName
    )
    {
        var template = await Mediator.Send(
            new GetWebTemplateByDeveloperNames.Query
            {
                ThemeDeveloperName = themeDeveloperName,
                TemplateDeveloperName = templateDeveloperName,
            }
        );
        var input = new DeleteWebTemplate.Command { Id = template.Result.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    private async Task<(object Target, ContentType_RenderModel? ContentType)> RenderViewTargetAsync(
        string viewId
    )
    {
        var view = await Mediator.Send(new GetViewById.Query { Id = viewId });
        var pageSize = view.Result.DefaultNumberOfItemsPerPage;
        var contentItems = await Mediator.Send(
            new GetContentItems.Query
            {
                ViewId = view.Result.Id,
                PageNumber = 1,
                PageSize = pageSize,
            }
        );
        var relations = await Mediator.Send(
            new GetWebTemplateContentItemRelationsByContentTypeId.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = view.Result.ContentTypeId,
            }
        );
        var templateNames = relations.Result.ToDictionary(
            relation => relation.ContentItemId,
            relation => relation.WebTemplate.DeveloperName
        );
        var list = ContentItemListResult_RenderModel.GetProjection(
            contentItems.Result,
            templateNames,
            view.Result,
            pageSize: pageSize,
            pageNumber: 1
        );
        return (list, ContentType_RenderModel.GetProjection(view.Result.ContentType));
    }
}
