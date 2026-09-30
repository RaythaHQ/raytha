using Raytha.Application.Login;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Security;

public sealed record ImpersonationParty(
    Guid Id,
    bool IsActive,
    bool IsAdmin,
    bool IsSuperAdmin,
    bool CanManageUsers
)
{
    public static ImpersonationParty Of(LoginDto user) =>
        new(
            user.Id.Guid,
            user.IsActive,
            user.IsAdmin,
            user.Roles.Any(r => r.DeveloperName == BuiltInRole.SuperAdmin.DeveloperName),
            user.Roles.Any(r =>
                r.SystemPermissions.Contains(BuiltInSystemPermission.MANAGE_USERS_PERMISSION)
            )
        );
}

/// <summary>
/// Who may sign in as someone else. Checked when a session starts and again on every request
/// while it lasts, so losing a role or being suspended ends the session. Returns the message to
/// show, or null.
/// </summary>
public static class ImpersonationRules
{
    public const string NotSignedIn = "You must be signed in as an administrator to impersonate.";
    public const string AlreadyImpersonating =
        "End the current impersonation before starting another.";
    public const string Self = "You cannot impersonate yourself.";
    public const string TargetInactive = "A suspended account cannot be impersonated.";
    public const string CallerNotAdmin = "Only an active administrator can impersonate.";
    public const string UsersPermissionRequired =
        "Impersonating a website user requires the Manage Users permission.";
    public const string SuperAdminOnly = "Only a super admin can impersonate an administrator.";

    public static string? Check(
        ImpersonationParty? caller,
        ImpersonationParty target,
        bool callerIsImpersonating
    )
    {
        if (caller is null)
            return NotSignedIn;
        if (callerIsImpersonating)
            return AlreadyImpersonating;
        if (caller.Id == target.Id)
            return Self;
        if (!target.IsActive)
            return TargetInactive;
        if (!caller.IsAdmin || !caller.IsActive)
            return CallerNotAdmin;
        if (target.IsAdmin && !caller.IsSuperAdmin)
            return SuperAdminOnly;
        if (!target.IsAdmin && !caller.CanManageUsers)
            return UsersPermissionRequired;
        return null;
    }
}
