using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentTypes;

/// <summary>
/// What every new content type needs besides its own rows, shared by creating a type and
/// importing a schema.
/// </summary>
public static class ContentTypeProvisioning
{
    /// <summary>
    /// Lets the active theme's templates that are open to new content types render this one, and
    /// returns them.
    /// </summary>
    public static async Task<WebTemplate[]> GrantDefaultTemplateAccessAsync(
        IRaythaDbContext db,
        Guid contentTypeId,
        CancellationToken cancellationToken
    )
    {
        var activeThemeId = await db
            .OrganizationSettings.Select(os => os.ActiveThemeId)
            .FirstAsync(cancellationToken);

        var defaultWebTemplates = await db
            .WebTemplates.Where(wt =>
                wt.ThemeId == activeThemeId && !wt.IsBaseLayout && wt.AllowAccessForNewContentTypes
            )
            .ToArrayAsync(cancellationToken);

        foreach (var webTemplate in defaultWebTemplates)
        {
            await db.WebTemplateAccessToModelDefinitions.AddAsync(
                new WebTemplateAccessToModelDefinition
                {
                    ContentTypeId = contentTypeId,
                    WebTemplateId = webTemplate.Id,
                },
                cancellationToken
            );
        }

        return defaultWebTemplates;
    }

    /// <summary>Gives every role that manages content types full access to the new type.</summary>
    public static void GrantRolePermissions(IRaythaDbContext db, Guid contentTypeId)
    {
        var roles = db
            .Roles.Include(p => p.ContentTypeRolePermissions)
            .Where(p => p.SystemPermissions.HasFlag(SystemPermissions.ManageContentTypes));
        foreach (var role in roles)
        {
            role.ContentTypeRolePermissions.Add(
                new ContentTypeRolePermission
                {
                    ContentTypeId = contentTypeId,
                    ContentTypePermissions = BuiltInContentTypePermission.AllPermissionsAsEnum,
                }
            );
        }
    }
}
