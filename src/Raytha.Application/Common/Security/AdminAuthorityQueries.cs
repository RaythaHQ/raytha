using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Security;

/// <summary>Loads the snapshots <see cref="AdminAuthorityGuard"/> decides over.</summary>
public static class AdminAuthorityQueries
{
    public static AdminAccount? FindAdminAccount(this IRaythaDbContext db, Guid? userId)
    {
        if (userId is null)
            return null;
        var user = db
            .Users.AsNoTracking()
            .Include(u => u.Roles)
            .ThenInclude(r => r.ContentTypeRolePermissions)
            .FirstOrDefault(u => u.Id == userId && u.IsAdmin);
        return user is null ? null : AdminAccount.Of(user);
    }

    public static AdminAccount? FindCaller(this IRaythaDbContext db, ICurrentUser currentUser) =>
        db.FindAdminAccount(currentUser.UserId?.Guid);

    /// <summary>Null when allowed, or when the target is not an admin (the handler reports 404).</summary>
    public static string? AccountActionDenial(
        this IRaythaDbContext db,
        ICurrentUser currentUser,
        Guid targetId,
        AdminAccountAction action
    )
    {
        var target = db.FindAdminAccount(targetId);
        return target is null
            ? null
            : AdminAuthorityGuard.CheckAccountAction(
                db.FindCaller(currentUser),
                target,
                action,
                db.CountActiveSuperAdmins()
            );
    }

    public static int CountActiveSuperAdmins(this IRaythaDbContext db)
    {
        string superAdmin = BuiltInRole.SuperAdmin;
        return db.Users.Count(u =>
            u.IsAdmin && u.IsActive && u.Roles.Any(r => r.DeveloperName == superAdmin)
        );
    }

    public static IReadOnlyList<RoleGrant> FindRoleGrants(
        this IRaythaDbContext db,
        IEnumerable<Guid> roleIds
    )
    {
        var ids = roleIds.Distinct().ToList();
        return db
            .Roles.AsNoTracking()
            .Include(r => r.ContentTypeRolePermissions)
            .Where(r => ids.Contains(r.Id))
            .AsEnumerable()
            .Select(RoleGrant.Of)
            .ToList();
    }
}
