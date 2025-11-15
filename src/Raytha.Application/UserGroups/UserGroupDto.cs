using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.UserGroups;

public record UserGroupDto : BaseAuditableEntityDto
{
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;

    public static Expression<Func<UserGroup, UserGroupDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static UserGroupDto GetProjection(UserGroup entity)
    {
        if (entity == null)
            return null;

        return new UserGroupDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}
