using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Api.Controllers.V1;

[AbsoluteMediaUrls]
public class ContentItemsController : BaseController
{
    [HttpGet($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}", Name = "GetContentItems")]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION
    )]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<ContentItemDto>>>
    > GetContentItems(
        string contentTypeDeveloperName,
        string viewId = "",
        string search = "",
        string filter = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetContentItems.Query
        {
            ContentType = contentTypeDeveloperName,
            ViewId = viewId,
            Search = search,
            Filter = filter,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        var response =
            await Mediator.Send(input) as QueryResponseDto<ListResultDto<ContentItemDto>>;
        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/trash",
        Name = "GetDeletedContentItems"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION
    )]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<ContentItemDto>>>
    > GetDeletedContentItems(string contentTypeDeveloperName)
    {
        var input = new GetDeletedContentItems.Query { DeveloperName = contentTypeDeveloperName };
        var response =
            await Mediator.Send(input) as QueryResponseDto<ListResultDto<ContentItemDto>>;
        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}",
        Name = "GetContentItemById"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION
    )]
    public async Task<ActionResult<IQueryResponseDto<ContentItemDto>>> GetContentItemById(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new GetContentItemById.Query { Id = contentItemId };
        var response = await Mediator.Send(input) as QueryResponseDto<ContentItemDto>;
        return response;
    }

    [HttpPost($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}", Name = "CreateContentItem")]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateContentItem(
        string contentTypeDeveloperName,
        [FromBody] CreateContentItem.Command request
    )
    {
        var input = request with { ContentTypeDeveloperName = contentTypeDeveloperName };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetContentItemById),
            new { contentTypeDeveloperName, contentItemId = response.Result },
            response
        );
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}",
        Name = "EditContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditContentItem(
        string contentTypeDeveloperName,
        string contentItemId,
        [FromBody] EditContentItem.Command request
    )
    {
        var input = request with { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}/settings",
        Name = "EditContentItemSettings"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditContentItemSettings(
        string contentTypeDeveloperName,
        string contentItemId,
        [FromBody] EditContentItemSettings.Command request
    )
    {
        var input = request with { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}/unpublish",
        Name = "UnpublishContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> UnpublishContentItem(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new UnpublishContentItem.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/items",
        Name = "DeleteContentItems"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteContentItems(
        string contentTypeDeveloperName,
        [FromBody] DeleteContentItems.Command request
    )
    {
        var contentType = await Mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );
        var response = await Mediator.Send(request with { ContentTypeId = contentType.Result.Id });
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    /// <summary>
    /// Queues the creation of many items of this content type and returns the background task id
    /// (202). Poll <c>GET /raytha/api/v1/BackgroundTasks/{id}</c> until <c>status</c> is
    /// <c>complete</c> or <c>error</c>. Each item is created like a single create, so one failing
    /// does not stop the rest. When the task is complete, <c>statusInfo</c> is JSON:
    /// <c>{ total, created, failed, items: [{ index, success, id, errors: [{ field, message }] }] }</c>,
    /// with <c>index</c> the item's position in the request.
    /// A relationship field may hold an item id, a route path, or the primary field value of the
    /// related item. An item may also reference another item of the same batch by its primary
    /// field value; that item is created first. Items cannot be sent in more than
    /// <c>500</c> per request.
    /// </summary>
    [HttpPost($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/batch", Name = "BatchCreateContentItems")]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> BatchCreateContentItems(
        string contentTypeDeveloperName,
        [FromBody] BeginBatchCreateContentItems.Command request
    )
    {
        var input = request with { ContentTypeDeveloperName = contentTypeDeveloperName };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Accepted(response);
    }

    [HttpPost(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/template",
        Name = "AssignContentItemTemplates"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> AssignContentItemTemplates(
        string contentTypeDeveloperName,
        [FromBody] AssignContentItemTemplates.Command request
    )
    {
        var contentType = await Mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );
        var input = request with { ContentTypeId = contentType.Result.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}",
        Name = "DeleteContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteContentItem(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new DeleteContentItem.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}/set-as-home-page",
        Name = "SetContentItemAsHomePage"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetContentItemAsHomePage(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new SetAsHomePage.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}/discard-draft",
        Name = "DiscardDraftContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DiscardDraftContentItem(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new DiscardDraftContentItem.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/{{contentItemId}}/restore",
        Name = "RestoreContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> RestoreContentItem(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new RestoreContentItem.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/trash/{{contentItemId}}",
        Name = "DeleteAlreadyDeletedContentItem"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteAlreadyDeletedContentItem(
        string contentTypeDeveloperName,
        string contentItemId
    )
    {
        var input = new DeleteAlreadyDeletedContentItem.Command { Id = contentItemId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/route/{{routePath}}",
        Name = "GetRouteByPath"
    )]
    [Authorize(
        Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
            + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION
    )]
    public async Task<ActionResult<IQueryResponseDto<RouteDto>>> GetRouteByPath(
        string contentTypeDeveloperName,
        string routePath
    )
    {
        var input = new GetRouteByPath.Query { Path = routePath };
        var response = await Mediator.Send(input) as QueryResponseDto<RouteDto>;
        return response;
    }
}
