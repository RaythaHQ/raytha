using FluentValidation;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Security;

public sealed record RoleGrant(Guid Id, string Label, string DeveloperName, PermissionGrant Permissions)
{
    public bool IsSuperAdmin => DeveloperName == BuiltInRole.SuperAdmin.DeveloperName;

    public static RoleGrant Of(Role role) =>
        new(role.Id, role.Label, role.DeveloperName, PermissionGrant.Of(role));
}

public sealed record AdminAccount(Guid Id, bool IsActive, IReadOnlyList<RoleGrant> Roles)
{
    public bool IsSuperAdmin => Roles.Any(r => r.IsSuperAdmin);

    public PermissionGrant Permissions { get; } = PermissionGrant.Union(Roles.Select(r => r.Permissions));

    public static AdminAccount Of(User user) =>
        new(user.Id, user.IsActive, user.Roles.Select(RoleGrant.Of).ToList());
}

public enum AdminAccountAction
{
    Edit,
    Suspend,
    Restore,
    Delete,
    ResetPassword,
    RemoveAccess,
    ManageApiKeys,
}

/// <summary>
/// Who may change administrators and roles, on top of the <c>administrators</c> endpoint policy.
/// The policy only says "may manage admins"; these rules stop that grant from becoming a path to
/// more power than the caller already holds. Every rule returns the message to show, or null.
/// </summary>
public static class AdminAuthorityGuard
{
    public const string NotSignedIn = "You must be signed in as an administrator to do this.";
    public const string SuperAdminAccountsOnly =
        "Only a super admin can manage an account that holds the Super Admin role.";
    public const string AccountAboveCaller =
        "You cannot manage an administrator who has permissions you do not have.";
    public const string SuperAdminRoleOnly =
        "Only a super admin can assign or remove the Super Admin role.";
    public const string OwnRoles = "You cannot change your own roles.";
    public const string LastSuperAdminRole =
        "The last active super admin cannot lose the Super Admin role.";
    public const string SuperAdminRoleLocked = "The Super Admin role cannot be edited.";
    public const string SuperAdminRoleUndeletable = "The Super Admin role cannot be deleted.";
    public const string UnknownRole = "One or more of the selected roles no longer exist.";

    private static readonly Dictionary<AdminAccountAction, string> ForbiddenOnSelf = new()
    {
        [AdminAccountAction.Delete] = "You cannot remove your own account.",
        [AdminAccountAction.Suspend] = "You cannot change the status on your own account.",
        [AdminAccountAction.Restore] = "You cannot change the status on your own account.",
        [AdminAccountAction.RemoveAccess] = "You cannot remove your own admin access.",
    };

    private static readonly Dictionary<AdminAccountAction, string> ForbiddenOnLastSuperAdmin = new()
    {
        [AdminAccountAction.Delete] = "The last active super admin cannot be deleted.",
        [AdminAccountAction.Suspend] = "The last active super admin cannot be suspended.",
        [AdminAccountAction.RemoveAccess] =
            "The last active super admin cannot have admin access removed.",
    };

    public static string? CheckAccountAction(
        AdminAccount? caller,
        AdminAccount target,
        AdminAccountAction action,
        int activeSuperAdmins
    )
    {
        if (caller is null)
            return NotSignedIn;
        if (caller.Id == target.Id)
            return ForbiddenOnSelf.GetValueOrDefault(action);
        if (target.IsSuperAdmin && !caller.IsSuperAdmin)
            return SuperAdminAccountsOnly;
        if (!caller.IsSuperAdmin && !target.Permissions.IsWithin(caller.Permissions))
            return AccountAboveCaller;
        if (IsLastActiveSuperAdmin(target, activeSuperAdmins))
            return ForbiddenOnLastSuperAdmin.GetValueOrDefault(action);
        return null;
    }

    /// <param name="target">The account being edited, or null when creating one.</param>
    public static string? CheckRoleAssignment(
        AdminAccount? caller,
        AdminAccount? target,
        IReadOnlyCollection<RoleGrant> requested,
        int activeSuperAdmins
    )
    {
        if (caller is null)
            return NotSignedIn;

        var current = target?.Roles ?? [];
        var added = requested.Where(r => current.All(c => c.Id != r.Id)).ToList();
        var removed = current.Where(c => requested.All(r => r.Id != c.Id)).ToList();
        if (added.Count == 0 && removed.Count == 0)
            return null;

        if (target is not null && target.Id == caller.Id)
            return OwnRoles;
        if (!caller.IsSuperAdmin && added.Concat(removed).Any(r => r.IsSuperAdmin))
            return SuperAdminRoleOnly;
        if (!caller.IsSuperAdmin)
        {
            var beyond = added.FirstOrDefault(r => !r.Permissions.IsWithin(caller.Permissions));
            if (beyond is not null)
                return $"You cannot assign the {beyond.Label} role because it grants permissions you do not have.";
        }
        if (
            target is not null
            && removed.Any(r => r.IsSuperAdmin)
            && IsLastActiveSuperAdmin(target, activeSuperAdmins)
        )
            return LastSuperAdminRole;
        return null;
    }

    /// <param name="existing">The role being edited, or null when creating one.</param>
    public static string? CheckRoleDefinition(
        AdminAccount? caller,
        RoleGrant? existing,
        PermissionGrant requested
    )
    {
        if (caller is null)
            return NotSignedIn;
        if (existing is not null && existing.IsSuperAdmin)
            return SuperAdminRoleLocked;
        if (caller.IsSuperAdmin)
            return null;
        if (existing is not null && !existing.Permissions.IsWithin(caller.Permissions))
            return $"You cannot edit the {existing.Label} role because it grants permissions you do not have.";
        if (!requested.IsWithin(caller.Permissions))
            return DescribeGrantBeyond(requested, caller.Permissions);
        return null;
    }

    public static string? CheckRoleDeletion(AdminAccount? caller, RoleGrant role)
    {
        if (caller is null)
            return NotSignedIn;
        if (role.IsSuperAdmin)
            return SuperAdminRoleUndeletable;
        if (!caller.IsSuperAdmin && !role.Permissions.IsWithin(caller.Permissions))
            return $"You cannot delete the {role.Label} role because it grants permissions you do not have.";
        return null;
    }

    public static void AddDenial<T>(this ValidationContext<T> context, string? denial)
    {
        if (denial is not null)
            context.AddFailure(Constants.VALIDATION_SUMMARY, denial);
    }

    private static bool IsLastActiveSuperAdmin(AdminAccount target, int activeSuperAdmins) =>
        target.IsSuperAdmin && target.IsActive && activeSuperAdmins <= 1;

    private static string DescribeGrantBeyond(PermissionGrant requested, PermissionGrant ceiling)
    {
        var missing = BuiltInSystemPermission
            .From(requested.SystemBeyond(ceiling))
            .Select(p => p.Label)
            .ToList();
        if (requested.HasContentTypesBeyond(ceiling))
            missing.Add("content type access you do not hold");
        return $"You can only grant permissions you have yourself. Remove: {string.Join(", ", missing)}.";
    }
}
