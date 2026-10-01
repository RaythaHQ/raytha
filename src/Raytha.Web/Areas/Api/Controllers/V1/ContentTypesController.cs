using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Authentication;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Api.Controllers.V1;

public record ContentTypeFieldTypeResponse(string Label, string DeveloperName);

[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION
)]
public class ContentTypesController : BaseController
{
    [HttpGet("", Name = "GetContentTypes")]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<ContentTypeListItemDto>>>
    > GetContentTypes([FromQuery] GetContentTypesAsListItems.Query request)
    {
        var response =
            await Mediator.Send(request) as QueryResponseDto<ListResultDto<ContentTypeListItemDto>>;
        return response;
    }

    [HttpGet(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}",
        Name = "GetContentTypeByDeveloperName"
    )]
    public async Task<
        ActionResult<IQueryResponseDto<ContentTypeDto>>
    > GetContentTypeByDeveloperName(string contentTypeDeveloperName)
    {
        var input = new GetContentTypeByDeveloperName.Query
        {
            DeveloperName = contentTypeDeveloperName,
        };
        var response = await Mediator.Send(input) as QueryResponseDto<ContentTypeDto>;
        return response;
    }

    [HttpGet("field-types", Name = "GetContentTypeFieldTypes")]
    public ActionResult<IQueryResponseDto<IEnumerable<ContentTypeFieldTypeResponse>>> GetContentTypeFieldTypes()
    {
        var fieldTypes = BaseFieldType
            .SupportedTypes.Select(t => new ContentTypeFieldTypeResponse(t.Label, t.DeveloperName))
            .ToArray();
        return new QueryResponseDto<IEnumerable<ContentTypeFieldTypeResponse>>(fieldTypes);
    }

    /// <summary>
    /// The whole content model as one document: every content type with its fields, choices, and
    /// views. Types are in developer name order and fields in display order, so two exports of
    /// the same model are identical. Send the document back to <c>POST contenttypes/import</c> to
    /// recreate the model on another site.
    /// </summary>
    [HttpGet("export", Name = "ExportSchema")]
    public async Task<ActionResult<IQueryResponseDto<SchemaDocument>>> ExportSchema()
    {
        return Ok(await Mediator.Send(new ExportSchema.Query()));
    }

    /// <summary>
    /// Applies a schema document (the body of <c>GET contenttypes/export</c>): content types,
    /// fields, choices, and views are created when missing and updated when present, matched by
    /// developer name. Nothing is deleted and a field's type never changes. It is applied
    /// completely or not at all. With <c>dryRun=true</c> it reports what would change and applies
    /// nothing.
    /// </summary>
    [HttpPost("import", Name = "ImportSchema")]
    public async Task<ActionResult<ICommandResponseDto<ImportSchema.ImportResult>>> ImportSchema(
        [FromBody] SchemaDocument schema,
        [FromQuery] bool dryRun = false
    )
    {
        var response = await Mediator.Send(
            new ImportSchema.Command { Schema = schema, DryRun = dryRun }
        );
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPost("", Name = "CreateContentType")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateContentType(
        [FromBody] CreateContentType.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetContentTypeByDeveloperName),
            new { contentTypeDeveloperName = request.DeveloperName.ToDeveloperName() },
            response
        );
    }

    [HttpDelete(
        $"{{{RouteConstants.CONTENT_TYPE_DEVELOPER_NAME}}}",
        Name = "DeleteContentType"
    )]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteContentType(
        string contentTypeDeveloperName
    )
    {
        var contentType = await Mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );
        var response = await Mediator.Send(
            new DeleteContentType.Command { Id = contentType.Result.Id }
        );
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}
