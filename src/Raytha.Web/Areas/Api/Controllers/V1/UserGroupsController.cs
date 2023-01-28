using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.UserGroups;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class UserGroupsController : BaseController
{
    [HttpGet("", Name = "GetUserGroups")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<UserGroupDto>>>> GetUserGroups(
                                           [FromQuery] GetUserGroups.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<UserGroupDto>>;
        return response;
    }

    [HttpGet("{userGroupId}", Name = "GetUserGroupById")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<UserGroupDto>>> GetUserGroupById(
                                        string userGroupId)
    {
        var input = new GetUserGroupById.Query { Id = userGroupId };
        var response = await Mediator.Send(input) as QueryResponseDto<UserGroupDto>;
        return response;
    }

    [HttpPost("", Name = "CreateUserGroup")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateUserGroup(
                                        [FromBody] CreateUserGroup.Command request)
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(nameof(GetUserGroupById), new { userGroupId = response.Result }, response);
    }

    [HttpPut("{userGroupId}", Name = "EditUserGroup")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditUserGroup(
                                        string userGroupId,
                                        [FromBody] EditUserGroup.Command request)
    {
        var input = request with { Id = userGroupId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{userGroupId}", Name = "DeleteUserGroup")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteUserGroup(
                                        string userGroupId)
    {
        var input = new DeleteUserGroup.Command { Id = userGroupId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}