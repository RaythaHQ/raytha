using Raytha.Domain.Entities;
using System.Linq.Expressions;
using Raytha.Application.Common.Models;

namespace Raytha.Application.NavigationMenus;

public record NavigationMenuDto : BaseAuditableEntityDto
{
    public required string Label { get; init; }
    public required string DeveloperName { get; init; }
    public bool IsMainMenu { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<NavigationMenu, NavigationMenuDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static NavigationMenuDto GetProjection(NavigationMenu entity)
    {
        return new NavigationMenuDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            IsMainMenu = entity.IsMainMenu,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
        };
    }
}