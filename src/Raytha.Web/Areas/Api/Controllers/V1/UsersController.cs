using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Models;
using Raytha.Application.Users;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Utils;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Api.Controllers.V1;


public class UsersController : BaseController
{
    [HttpGet("", Name = "GetUsers")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<UserDto>>>> GetUsers(
                                           [FromQuery] GetUsers.Query request)
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<UserDto>>;
        return response;
    }

    [HttpGet("{userId}", Name = "GetUserById")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<IQueryResponseDto<UserDto>>> GetUserById(
                                        string userId)
    {
        var input = new GetUserById.Query { Id = userId };
        var response = await Mediator.Send(input) as QueryResponseDto<UserDto>;
        return response;
    }

    [HttpPost("", Name = "CreateUser")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateUser(
                                        [FromBody] CreateUser.Command request)
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(nameof(GetUserById), new { userId = response.Result }, response);
    }

    [HttpPut("{userId}", Name = "EditUser")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditUser(
                                        string userId,
                                        [FromBody] EditUser.Command request)
    {
        var input = request with { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{userId}", Name = "DeleteUser")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteUser(
                                        string userId)
    {
        var input = new DeleteUser.Command { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{userId}/password", Name = "ResetPassword")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> ResetPassword(
                                        string userId,
                                        [FromBody] ResetPassword_RequestModel request)
    {
        var input = new ResetPassword.Command 
        { 
            Id = userId, 
            SendEmail = request.SendEmail, 
            NewPassword = request.NewPassword, 
            ConfirmNewPassword = request.NewPassword 
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{userId}/is-active", Name = "SetIsActive")]
    [Authorize(Policy = RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetIsActive(
                                        string userId,
                                        [FromBody] SetIsActive.Command request)
    {
        var input = request with { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}

public record ResetPassword_RequestModel
{
    public string NewPassword { get; init; }
    public bool SendEmail { get; init; } = true;
}