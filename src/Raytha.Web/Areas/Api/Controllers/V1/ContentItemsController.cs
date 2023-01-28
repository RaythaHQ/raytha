using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class ContentItemsController : BaseController
{
    [HttpGet("{contentType}", Name = "GetContentItems")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<ContentItemDto>>>> GetContentItems(
                                           string contentType,
                                           string viewId = "",
                                           string search = "",
                                           string filter = "",
                                           string orderBy = "",
                                           int pageNumber = 1,
                                           int pageSize = 50)
    {
        if (!string.IsNullOrEmpty(viewId) && ShortGuid.TryParse(viewId, out ShortGuid shortGuid))
        {
            return BadRequest(new { success = false, error = "Invalid format of view id." });
        }
        var input = new GetContentItems.Query
        {
            Search = search,
            ContentType = contentType,
            ViewId = viewId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
            Filter = filter
        };
        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<ContentItemDto>>;
        return response;
    }

    [HttpGet("{contentType}/{contentItemId}", Name = "GetContentItemById")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ContentItemDto>>> GetContentItemById(
                                       string contentType,
                                       string contentItemId)
    {
        ShortGuid shortGuid;
        if (!ShortGuid.TryParse(contentItemId, out shortGuid))
        {
            return BadRequest(new { success = false, error = "Invalid format of content item id." });
        }
        var input = new GetContentItemById.Query { Id = shortGuid };
        var response = await Mediator.Send(input) as QueryResponseDto<ContentItemDto>;
        return response;
    }

    [HttpPost("{contentType}", Name = "CreateContentItem")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateContentItem(string contentType, [FromBody] CreateContentItem.Command request)
    {
        var contentTypeResponse = await Mediator.Send(new GetContentTypeByDeveloperName.Query { DeveloperName = contentType });
        var input = request with { ContentTypeId = contentTypeResponse.Result.Id };
        var response = await Mediator.Send(input);
        return response;
    }

    [HttpGet("{contentType}/route/{routePath}", Name = "GetRouteByPath")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<RouteDto>>> GetRouteByPath(
                                       string routePath)
    {
        var input = new GetRouteByPath.Query
        {
            Path = routePath
        };
        var response = await Mediator.Send(input) as QueryResponseDto<RouteDto>;
        return response;
    }
}
