using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Commands;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Api.Controllers.V1;

/// <summary>
/// Per content type configuration: the content type itself, its fields and its views. There is no
/// class-level policy because these routes are authorized per content type, which
/// <see cref="RaythaApiAuthorizationHandler"/> reads from the contentTypeDeveloperName route value.
/// </summary>
[Route("raytha/api/v1/contenttypes")]
public class ContentTypeConfigurationController : BaseController
{
    private const string CONFIG_POLICY =
        RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION;
    private const string READ_POLICY =
        RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION;

    [HttpPut($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}", Name = "EditContentType")]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditContentType(
        string contentTypeDeveloperName,
        [FromBody] EditContentType.Command request
    )
    {
        var contentTypeId = await GetContentTypeId(contentTypeDeveloperName);
        var input = request with { Id = contentTypeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields", Name = "CreateContentTypeField")]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateContentTypeField(
        string contentTypeDeveloperName,
        [FromBody] CreateContentTypeField.Command request
    )
    {
        var contentTypeId = await GetContentTypeId(contentTypeDeveloperName);
        var input = request with { ContentTypeId = contentTypeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            "GetContentTypeByDeveloperName",
            "ContentTypes",
            new { contentTypeDeveloperName },
            response
        );
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/{{id}}",
        Name = "EditContentTypeField"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditContentTypeField(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditContentTypeField.Command request
    )
    {
        var fieldId = await GetFieldId(contentTypeDeveloperName, id);
        var input = request with { Id = fieldId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/{{id}}",
        Name = "DeleteContentTypeField"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteContentTypeField(
        string contentTypeDeveloperName,
        string id
    )
    {
        var fieldId = await GetFieldId(contentTypeDeveloperName, id);
        var input = new DeleteContentTypeField.Command { Id = fieldId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/fields/{{id}}/reorder",
        Name = "ReorderContentTypeField"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> ReorderContentTypeField(
        string contentTypeDeveloperName,
        string id,
        [FromBody] ReorderContentTypeField.Command request
    )
    {
        var fieldId = await GetFieldId(contentTypeDeveloperName, id);
        var input = request with { Id = fieldId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    /// <summary>
    /// The views of a content type. By default (<c>compact=true</c>) each view's embedded
    /// <c>contentType</c> carries its summary but not <c>contentTypeFields</c>, which repeats the
    /// whole field model in every view; read that once from <c>GET contenttypes/{name}</c>, or pass
    /// <c>compact=false</c> to get it inside each view.
    /// </summary>
    [HttpGet($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views", Name = "GetViews")]
    [Authorize(Policy = READ_POLICY)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<ViewDto>>>> GetViews(
        string contentTypeDeveloperName,
        string search = "",
        string? orderBy = null,
        int pageNumber = 1,
        int pageSize = 50,
        bool compact = true
    )
    {
        var input = new GetViews.Query
        {
            ContentTypeDeveloperName = contentTypeDeveloperName,
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        if (!string.IsNullOrEmpty(orderBy))
        {
            input = input with { OrderBy = orderBy };
        }
        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<ViewDto>>;
        if (!compact || response is null || !response.Success)
        {
            return response;
        }
        return new QueryResponseDto<ListResultDto<ViewDto>>(
            new ListResultDto<ViewDto>(
                response.Result.Items.Select(Compacted).ToList(),
                response.Result.TotalCount
            )
        );
    }

    /// <summary>
    /// One view. <c>compact=true</c> (the default) leaves <c>contentType.contentTypeFields</c> out,
    /// as for the list; <c>compact=false</c> includes it.
    /// </summary>
    [HttpGet($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}", Name = "GetViewById")]
    [Authorize(Policy = READ_POLICY)]
    public async Task<ActionResult<IQueryResponseDto<ViewDto>>> GetViewById(
        string contentTypeDeveloperName,
        string id,
        bool compact = true
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        return new QueryResponseDto<ViewDto>(compact ? Compacted(view) : view);
    }

    [HttpPost($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views", Name = "CreateView")]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateView(
        string contentTypeDeveloperName,
        [FromBody] CreateView.Command request
    )
    {
        var contentTypeId = await GetContentTypeId(contentTypeDeveloperName);
        var input = request with { ContentTypeId = contentTypeId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetViewById),
            new { contentTypeDeveloperName, id = response.Result },
            response
        );
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}/public-settings",
        Name = "EditViewPublicSettings"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditViewPublicSettings(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditPublicSettings.Command request
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = request with { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}/filter",
        Name = "EditViewFilter"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditViewFilter(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditFilter.Command request
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = request with { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}/sort",
        Name = "EditViewSort"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditViewSort(
        string contentTypeDeveloperName,
        string id,
        [FromBody] EditSort.Command request
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = request with { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}/sort/reorder",
        Name = "ReorderViewSort"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> ReorderViewSort(
        string contentTypeDeveloperName,
        string id,
        [FromBody] ReorderSort.Command request
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = request with { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete($"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}", Name = "DeleteView")]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteView(
        string contentTypeDeveloperName,
        string id
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = new DeleteView.Command { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}/views/{{id}}/set-as-home-page",
        Name = "SetViewAsHomePage"
    )]
    [Authorize(Policy = CONFIG_POLICY)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetViewAsHomePage(
        string contentTypeDeveloperName,
        string id
    )
    {
        var view = await GetView(contentTypeDeveloperName, id);
        var input = new Raytha.Application.Views.Commands.SetAsHomePage.Command { Id = view.Id };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    private async Task<ShortGuid> GetContentTypeId(string contentTypeDeveloperName)
    {
        var response = await Mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );
        return response.Result.Id;
    }

    /// <summary>
    /// The route's content type is what the caller was authorized against, so a field or view id
    /// that belongs to another content type is a 404 rather than a way around that policy.
    /// </summary>
    private async Task<ShortGuid> GetFieldId(string contentTypeDeveloperName, string id)
    {
        var contentTypeId = await GetContentTypeId(contentTypeDeveloperName);
        var fields = await Mediator.Send(
            new GetContentTypeFields.Query { ContentTypeId = contentTypeId, PageSize = int.MaxValue }
        );
        if (
            !ShortGuid.TryParse(id, out ShortGuid fieldId) || fields.Result.Items.All(f => f.Id != fieldId)
        )
        {
            throw new NotFoundException("Content Type Field", id);
        }
        return fieldId;
    }

    /// <summary>The view without its content type's field definitions; the type's summary stays.</summary>
    private static ViewDto Compacted(ViewDto view) =>
        view.ContentType is null
            ? view
            : view with
            {
                ContentType = view.ContentType with { ContentTypeFields = null },
            };

    private async Task<ViewDto> GetView(string contentTypeDeveloperName, string id)
    {
        var contentTypeId = await GetContentTypeId(contentTypeDeveloperName);
        if (!ShortGuid.TryParse(id, out ShortGuid viewId))
        {
            throw new NotFoundException("View", id);
        }
        var response = await Mediator.Send(new GetViewById.Query { Id = viewId });
        if (response.Result.ContentTypeId != contentTypeId)
        {
            throw new NotFoundException("View", id);
        }
        return response.Result;
    }
}
