using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>Users and user groups (<c>MANAGE_USERS_PERMISSION</c>).</summary>
public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsers(this RouteGroupBuilder admin)
    {
        var users = admin.MapGroup("/users")
            .WithTags("Admin users")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_USERS_PERMISSION);

        users.MapGet("", ListUsers);
        users.MapGet("/{id}", GetUser);
        users.MapPost("", CreateUserHandler);
        users.MapPut("/{id}", EditUserHandler);
        users.MapDelete("/{id}", DeleteUserHandler);
        users.MapPost("/{id}/suspend", (string id, ISender m) => SetActive(id, false, m));
        users.MapPost("/{id}/restore", (string id, ISender m) => SetActive(id, true, m));
        users.MapPost("/{id}/reset-password", ResetPasswordHandler);

        var groups = admin.MapGroup("/user-groups")
            .WithTags("Admin user groups")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_USERS_PERMISSION);

        groups.MapGet("", ListGroups);
        groups.MapGet("/{id}", GetGroup);
        groups.MapPost("", CreateGroup);
        groups.MapPut("/{id}", EditGroup);
        groups.MapDelete("/{id}", DeleteGroup);

        return admin;
    }

    private static async Task<IResult> ListUsers([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetUsers.Query
        {
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetUser(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetUserById.Query { Id = id }));

    private static async Task<IResult> CreateUserHandler([FromBody] CreateUser.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditUserHandler(string id, [FromBody] EditUser.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteUserHandler(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteUser.Command { Id = id }));

    private static async Task<IResult> SetActive(string id, bool isActive, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetIsActive.Command { Id = id, IsActive = isActive }));

    private static async Task<IResult> ResetPasswordHandler(
        string id,
        [FromBody] ResetPasswordRequest body,
        ISender mediator
    )
    {
        var response = await mediator.Send(
            new ResetPassword.Command
            {
                Id = id,
                NewPassword = body.NewPassword ?? string.Empty,
                ConfirmNewPassword = body.ConfirmNewPassword ?? body.NewPassword ?? string.Empty,
                SendEmail = body.SendEmail ?? true,
            }
        );
        return AdminResults.NoContent(response);
    }

    private static async Task<IResult> ListGroups([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetUserGroups.Query
        {
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetGroup(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetUserGroupById.Query { Id = id }));

    private static async Task<IResult> CreateGroup([FromBody] CreateUserGroup.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditGroup(string id, [FromBody] EditUserGroup.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteGroup(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteUserGroup.Command { Id = id }));

    public sealed record ResetPasswordRequest(string? NewPassword, string? ConfirmNewPassword, bool? SendEmail);
}
