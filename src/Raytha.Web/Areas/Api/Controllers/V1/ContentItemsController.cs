using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class ContentItemsController : BaseController
{
    [HttpGet($"{{contentType}}", Name = "GetContentItems")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<ContentItemDto>>>> Get(
                                           string contentType,
                                           string viewId = "",
                                           string search = "",
                                           string filter = "",
                                           string orderBy = "",
                                           int pageNumber = 1,
                                           int pageSize = 50)
    {
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
}
