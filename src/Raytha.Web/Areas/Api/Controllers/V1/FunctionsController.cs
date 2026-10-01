using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.RaythaFunctions;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Api.Controllers.V1;

/// <summary>
/// Raytha Functions: the JavaScript an HTTP request, Liquid template, or content item event runs.
/// Functions are addressed by developer name, which may contain dots (<c>llms.txt</c>) and cannot
/// change. Same permission as the admin Functions pages: Manage System Settings.
/// </summary>
[Authorize(
    Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION
)]
public class FunctionsController : BaseController
{
    [HttpGet("", Name = "GetFunctions")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<RaythaFunctionDto>>>> GetFunctions(
        string search = "",
        string? orderBy = null,
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetRaythaFunctions.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        if (!string.IsNullOrEmpty(orderBy))
        {
            input = input with { OrderBy = orderBy };
        }
        var response = await Mediator.Send(input) as QueryResponseDto<ListResultDto<RaythaFunctionDto>>;
        return response;
    }

    [HttpGet("{developerName}", Name = "GetFunctionByDeveloperName")]
    public async Task<ActionResult<IQueryResponseDto<RaythaFunctionDto>>> GetFunctionByDeveloperName(
        string developerName
    )
    {
        var response =
            await Mediator.Send(new GetRaythaFunctionByDeveloperName.Query { DeveloperName = developerName })
            as QueryResponseDto<RaythaFunctionDto>;
        return response;
    }

    /// <summary>
    /// <c>triggerType</c> is <c>http_request</c>, <c>liquid_template</c>, <c>content_item_created</c>,
    /// <c>content_item_updated</c>, or <c>content_item_deleted</c>. <c>routePath</c> (optional) is
    /// the public path an HTTP request function also answers at, such as <c>llms.txt</c>.
    /// </summary>
    [HttpPost("", Name = "CreateFunction")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateFunction(
        [FromBody] CreateRaythaFunction.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetFunctionByDeveloperName),
            new { developerName = request.DeveloperName.ToDeveloperName(allowDot: true) },
            response
        );
    }

    /// <summary>
    /// Replaces the function: send every field (<c>name</c>, <c>triggerType</c>, <c>isActive</c>,
    /// <c>code</c>, <c>routePath</c>). Changed code keeps the previous code as a revision, and an
    /// empty <c>routePath</c> removes the public path.
    /// </summary>
    [HttpPut("{developerName}", Name = "EditFunction")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditFunction(
        string developerName,
        [FromBody] EditRaythaFunction.Command request
    )
    {
        var function = await GetFunction(developerName);
        var response = await Mediator.Send(request with { Id = function.Id });
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{developerName}", Name = "DeleteFunction")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteFunction(string developerName)
    {
        var function = await GetFunction(developerName);
        var response = await Mediator.Send(new DeleteRaythaFunction.Command { Id = function.Id });
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    /// <summary>Earlier versions of the function's code, newest first.</summary>
    [HttpGet("{developerName}/revisions", Name = "GetFunctionRevisions")]
    public async Task<
        ActionResult<IQueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>>>
    > GetFunctionRevisions(
        string developerName,
        string? orderBy = null,
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var function = await GetFunction(developerName);
        var input = new GetRaythaFunctionRevisionsByRaythaFunctionId.Query
        {
            Id = function.Id,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };
        if (!string.IsNullOrEmpty(orderBy))
        {
            input = input with { OrderBy = orderBy };
        }
        var response =
            await Mediator.Send(input)
            as QueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>>;
        return response;
    }

    /// <summary>
    /// Makes a revision's code the function's code. The current code is saved as a new revision
    /// first, so a revert can itself be undone.
    /// </summary>
    [HttpPost("{developerName}/revisions/{revisionId}/revert", Name = "RevertFunction")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> RevertFunction(
        string developerName,
        string revisionId
    )
    {
        var function = await GetFunction(developerName);
        if (!ShortGuid.TryParse(revisionId, out ShortGuid revisionGuid))
        {
            throw new NotFoundException("Raytha Function Revision", revisionId);
        }
        var response = await Mediator.Send(
            new RevertRaythaFunction.Command { Id = revisionGuid, RaythaFunctionId = function.Id }
        );
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    private async Task<RaythaFunctionDto> GetFunction(string developerName)
    {
        var response = await Mediator.Send(
            new GetRaythaFunctionByDeveloperName.Query { DeveloperName = developerName }
        );
        return response.Result;
    }
}
