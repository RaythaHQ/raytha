using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles;

public record RoleDto : BaseAuditableEntityDto
{
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public IEnumerable<string> SystemPermissions { get; init; } = null!;
    public Dictionary<ShortGuid, IEnumerable<string>> ContentTypePermissions { get; init; } = null!;
    public Dictionary<
        string,
        IEnumerable<string>
    > ContentTypePermissionsFriendlyNames
    { get; init; } = null!;

    public static Expression<Func<Role, RoleDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static RoleDto GetProjection(Role entity)
    {
        var contentTypePermissions = new Dictionary<ShortGuid, IEnumerable<string>>();
        var contentTypePermissionsFriendlyNames = new Dictionary<string, IEnumerable<string>>();
        if (entity.ContentTypeRolePermissions != null)
        {
            foreach (var contentTypePerm in entity.ContentTypeRolePermissions)
            {
                var listOfPermissions = BuiltInContentTypePermission
                    .From(contentTypePerm.ContentTypePermissions)
                    .Select(p => p.DeveloperName);
                contentTypePermissions.Add(contentTypePerm.ContentTypeId, listOfPermissions);
                contentTypePermissionsFriendlyNames.Add(
                    contentTypePerm.ContentType.DeveloperName,
                    listOfPermissions
                );
            }
        }

        return new RoleDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            SystemPermissions = BuiltInSystemPermission
                .From(entity.SystemPermissions)
                .Select(p => p.DeveloperName),
            ContentTypePermissions = contentTypePermissions,
            ContentTypePermissionsFriendlyNames = contentTypePermissionsFriendlyNames,
        };
    }
}
