using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenus;

public record NavigationMenuRevisionDto : BaseAuditableEntityDto
{
    public required string NavigationMenuItemsJson { get; init; }
    public required ShortGuid NavigationMenuId { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<NavigationMenuRevision, NavigationMenuRevisionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static NavigationMenuRevisionDto GetProjection(NavigationMenuRevision entity)
    {
        return new NavigationMenuRevisionDto
        {
            Id = entity.Id,
            NavigationMenuId = entity.NavigationMenuId,
            NavigationMenuItemsJson = entity.NavigationMenuItemsJson,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModifierUserId = entity.LastModifierUserId,
            LastModificationTime = entity.LastModificationTime,
        };
    }
}