using CSharpVitamins;
using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Security;

/// <summary>
/// A set of system and per-content-type permissions: what one role grants, or the union a person
/// holds through all of their roles. Implied permissions are applied on construction, so every
/// instance is already normalized.
/// </summary>
public sealed class PermissionGrant
{
    public static readonly PermissionGrant None = new(
        SystemPermissions.None,
        new Dictionary<Guid, ContentTypePermissions>()
    );

    public PermissionGrant(
        SystemPermissions system,
        IEnumerable<KeyValuePair<Guid, ContentTypePermissions>> contentTypes
    )
    {
        System = BuiltInSystemPermission.WithImplied(system);
        ContentTypes = contentTypes
            .GroupBy(p => p.Key)
            .Select(g => new KeyValuePair<Guid, ContentTypePermissions>(
                g.Key,
                BuiltInContentTypePermission.WithImplied(
                    g.Aggregate(ContentTypePermissions.None, (all, p) => all | p.Value)
                )
            ))
            .Where(p => p.Value != ContentTypePermissions.None)
            .ToDictionary(p => p.Key, p => p.Value);
    }

    public SystemPermissions System { get; }
    public IReadOnlyDictionary<Guid, ContentTypePermissions> ContentTypes { get; }

    /// <summary>Manage Content Types authorizes every content type (RaythaAdminAuthorizationHandler).</summary>
    public bool CoversAllContentTypes => System.HasFlag(SystemPermissions.ManageContentTypes);

    public static PermissionGrant Of(Role role) =>
        new(
            role.SystemPermissions,
            (role.ContentTypeRolePermissions ?? []).Select(
                p => new KeyValuePair<Guid, ContentTypePermissions>(
                    p.ContentTypeId,
                    p.ContentTypePermissions
                )
            )
        );

    /// <summary>Parses the developer names a role create/edit request carries.</summary>
    public static PermissionGrant FromRequest(
        IEnumerable<string>? systemPermissions,
        IDictionary<string, IEnumerable<string>>? contentTypePermissions
    ) =>
        new(
            BuiltInSystemPermission.From((systemPermissions ?? []).ToArray()),
            (contentTypePermissions ?? new Dictionary<string, IEnumerable<string>>()).Select(
                p => new KeyValuePair<Guid, ContentTypePermissions>(
                    ((ShortGuid)p.Key).Guid,
                    BuiltInContentTypePermission.From((p.Value ?? []).ToArray())
                )
            )
        );

    public static PermissionGrant Union(IEnumerable<PermissionGrant> grants)
    {
        var list = grants.ToList();
        return new(
            list.Aggregate(SystemPermissions.None, (all, g) => all | g.System),
            list.SelectMany(g => g.ContentTypes)
        );
    }

    public ContentTypePermissions For(Guid contentTypeId) =>
        CoversAllContentTypes
            ? BuiltInContentTypePermission.AllPermissionsAsEnum
            : ContentTypes.GetValueOrDefault(contentTypeId, ContentTypePermissions.None);

    public bool IsWithin(PermissionGrant ceiling) =>
        SystemBeyond(ceiling) == SystemPermissions.None && !HasContentTypesBeyond(ceiling);

    public SystemPermissions SystemBeyond(PermissionGrant ceiling) => System & ~ceiling.System;

    public bool HasContentTypesBeyond(PermissionGrant ceiling) =>
        ContentTypes.Any(p => (p.Value & ~ceiling.For(p.Key)) != ContentTypePermissions.None);

    public IEnumerable<ContentTypeRolePermission> ToContentTypeRolePermissions() =>
        ContentTypes.Select(p => new ContentTypeRolePermission
        {
            ContentTypeId = p.Key,
            ContentTypePermissions = p.Value,
        });
}
