using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Routes;
using Raytha.Application.Routes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class ContentTypesController : BaseController
{
    [HttpGet("", Name = "GetContentTypes")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<ContentTypeListItemDto>>>> GetContentTypes(
                                           [FromQuery] GetContentTypesAsListItems.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<ContentTypeListItemDto>>;
        return response;
    }

    [HttpGet("{contentTypeDeveloperName}", Name = "GetContentTypeByDeveloperName")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ContentTypeDto>>> GetContentTypeByDeveloperName(
                                        string contentTypeDeveloperName)
    {
        var input = new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName };
        var response = await Mediator.Send(input) as QueryResponseDto<ContentTypeDto>;
        return response;
    }
}
