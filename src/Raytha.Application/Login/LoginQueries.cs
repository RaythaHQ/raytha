using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Login;

public static class LoginQueries
{
    /// <summary>The account as the cookie session sees it: roles, permissions, and groups.</summary>
    public static LoginDto? FindLogin(this IRaythaDbContext db, Guid userId)
    {
        var entity = db
            .Users.Include(p => p.Roles)
            .ThenInclude(p => p.ContentTypeRolePermissions)
            .ThenInclude(p => p.ContentType)
            .Include(p => p.UserGroups)
            .Include(p => p.AuthenticationScheme)
            .FirstOrDefault(p => p.Id == userId);
        return entity is null ? null : LoginDto.GetProjection(entity);
    }
}
