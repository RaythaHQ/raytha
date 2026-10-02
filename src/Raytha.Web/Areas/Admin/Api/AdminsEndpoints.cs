using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Roles.Commands;
using Raytha.Application.Roles.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>Administrators, their API keys, and roles (<c>MANAGE_ADMINISTRATORS_PERMISSION</c>).</summary>
public static class AdminsEndpoints
{
    public static RouteGroupBuilder MapAdmins(this RouteGroupBuilder admin)
    {
        var admins = admin.MapGroup("/admins")
            .WithTags("Admin administrators")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION);

        admins.MapGet("", ListAdmins);
        admins.MapGet("/{id}", GetAdmin);
        admins.MapPost("", CreateAdminHandler);
        admins.MapPut("/{id}", EditAdminHandler);
        admins.MapDelete("/{id}", DeleteAdminHandler);
        admins.MapPost("/{id}/suspend", (string id, ISender m) => SetActive(id, false, m));
        admins.MapPost("/{id}/restore", (string id, ISender m) => SetActive(id, true, m));
        admins.MapPost("/{id}/remove-access", RemoveAccess);
        admins.MapPost("/{id}/reset-password", ResetPasswordHandler);

        admins.MapGet("/{id}/api-keys", ListApiKeys);
        admins.MapPost("/{id}/api-keys", CreateApiKeyHandler);
        admins.MapDelete("/{id}/api-keys/{keyId}", DeleteApiKeyHandler);

        var roles = admin.MapGroup("/roles")
            .WithTags("Admin roles")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION);

        roles.MapGet("", ListRoles);
        roles.MapGet("/{id}", GetRole);
        roles.MapPost("", CreateRoleHandler);
        roles.MapPut("/{id}", EditRoleHandler);
        roles.MapDelete("/{id}", DeleteRoleHandler);
        roles.MapGet("/permissions", Permissions);

        return admin;
    }

    private static async Task<IResult> ListAdmins([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetAdmins.Query
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

    private static async Task<IResult> GetAdmin(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetAdminById.Query { Id = id }));

    private static async Task<IResult> CreateAdminHandler([FromBody] CreateAdmin.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditAdminHandler(string id, [FromBody] EditAdmin.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteAdminHandler(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteAdmin.Command { Id = id }));

    private static async Task<IResult> SetActive(string id, bool isActive, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new SetIsActive.Command { Id = id, IsActive = isActive }));

    private static async Task<IResult> RemoveAccess(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RemoveAdminAccess.Command { Id = id }));

    private static async Task<IResult> ResetPasswordHandler(
        string id,
        [FromBody] UsersEndpoints.ResetPasswordRequest body,
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

    private static async Task<IResult> ListApiKeys(string id, [AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetApiKeysForAdmin.Query
        {
            UserId = id,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
        };
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    /// <summary>Returns the plaintext key once; it is stored hashed and cannot be retrieved again.</summary>
    private static async Task<IResult> CreateApiKeyHandler(string id, ISender mediator)
    {
        var response = await mediator.Send(new CreateApiKey.Command { UserId = id });
        return response.Success
            ? Results.Created((string?)null, new { apiKey = response.Result })
            : AdminResults.Problem(response.GetErrors());
    }

    private static async Task<IResult> DeleteApiKeyHandler(string id, string keyId, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteApiKey.Command { Id = keyId }));

    private static async Task<IResult> ListRoles([AsParameters] PagedQuery paging, ISender mediator)
    {
        var query = new GetRoles.Query
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

    private static async Task<IResult> GetRole(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetRoleById.Query { Id = id }));

    private static async Task<IResult> CreateRoleHandler([FromBody] CreateRole.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body), created: true);

    private static async Task<IResult> EditRoleHandler(string id, [FromBody] EditRole.Command body, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteRoleHandler(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteRole.Command { Id = id }));

    /// <summary>Catalog of assignable permissions so the SPA role editor does not hardcode them.</summary>
    private static IResult Permissions()
    {
        return Results.Ok(
            new
            {
                systemPermissions = BuiltInSystemPermission
                    .Permissions.Select(p => new { label = p.Label, developerName = p.DeveloperName }),
                contentTypePermissions = BuiltInContentTypePermission
                    .Permissions.Select(p => new { label = p.Label, developerName = p.DeveloperName }),
            }
        );
    }
}
