using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.SitePages;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION
)]
[AbsoluteMediaUrls]
public class SitePagesController : BaseController
{
    [HttpGet("", Name = "GetSitePages")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<SitePageDto>>>> GetSitePages(
        string search = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetSitePages.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<SitePageDto>>;
        return response;
    }

    [HttpGet("{sitePageId}", Name = "GetSitePageById")]
    public async Task<ActionResult<IQueryResponseDto<SitePageDto>>> GetSitePageById(
        string sitePageId
    )
    {
        var input = new GetSitePageById.Query { Id = sitePageId };
        var response = await Mediator.Send(input) as QueryResponseDto<SitePageDto>;
        return response;
    }

    [HttpPost("", Name = "CreateSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateSitePage(
        [FromBody] CreateSitePage.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetSitePageById),
            new { sitePageId = response.Result },
            response
        );
    }

    [HttpPut("{sitePageId}", Name = "EditSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditSitePage(
        string sitePageId,
        [FromBody] EditSitePage.Command request
    )
    {
        var input = request with { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/settings", Name = "EditSitePageSettings")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditSitePageSettings(
        string sitePageId,
        [FromBody] EditSitePageSettings.Command request
    )
    {
        var input = request with { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/publish", Name = "PublishSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> PublishSitePage(
        string sitePageId
    )
    {
        var input = new PublishSitePage.Command { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/unpublish", Name = "UnpublishSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> UnpublishSitePage(
        string sitePageId
    )
    {
        var input = new UnpublishSitePage.Command { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpGet("widget-definitions", Name = "GetWidgetDefinitions")]
    public async Task<
        ActionResult<IQueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>>
    > GetWidgetDefinitions()
    {
        var input = new GetWidgetDefinitions.Query();
        var response =
            await Mediator.Send(input) as QueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>;
        return response;
    }

    [HttpPut("{sitePageId}/widgets", Name = "SaveSitePageWidgets")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SaveSitePageWidgets(
        string sitePageId,
        [FromBody] SaveWidgets.Command request
    )
    {
        var input = request with { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/widgets/{widgetId}", Name = "EditSitePageWidget")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditSitePageWidget(
        string sitePageId,
        string widgetId,
        [FromBody] EditWidget.Command request
    )
    {
        var input = request with { SitePageId = sitePageId, WidgetId = widgetId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{sitePageId}/widgets/{widgetId}", Name = "DeleteSitePageWidget")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteSitePageWidget(
        string sitePageId,
        string widgetId,
        string sectionName
    )
    {
        var input = new DeleteWidget.Command
        {
            SitePageId = sitePageId,
            WidgetId = widgetId,
            SectionName = sectionName,
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/discard-draft", Name = "DiscardDraftSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DiscardDraftSitePage(
        string sitePageId
    )
    {
        var input = new DiscardDraftSitePage.Command { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{sitePageId}/set-as-home-page", Name = "SetSitePageAsHomePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetSitePageAsHomePage(
        string sitePageId
    )
    {
        var input = new SetSitePageAsHomePage.Command { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{sitePageId}", Name = "DeleteSitePage")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteSitePage(
        string sitePageId
    )
    {
        var input = new DeleteSitePage.Command { Id = sitePageId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}
