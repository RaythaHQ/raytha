using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenuItems;

public record NavigationMenuItemDto : BaseAuditableEntityDto
{
    public required string Label { get; init; }
    public required string Url { get; init; }
    public bool IsDisabled { get; init; }
    public bool OpenInNewTab { get; init; }
    public string? CssClassName { get; init; }
    public int Ordinal { get; init; }
    public ShortGuid? ParentNavigationMenuItemId { get; init; }
    public required ShortGuid NavigationMenuId { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<NavigationMenuItem, NavigationMenuItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static NavigationMenuItemDto GetProjection(NavigationMenuItem entity)
    {
        return new NavigationMenuItemDto
        {
            Id = entity.Id,
            Label = entity.Label,
            Url = entity.Url,
            IsDisabled = entity.IsDisabled,
            OpenInNewTab = entity.OpenInNewTab,
            CssClassName = entity.CssClassName,
            Ordinal = entity.Ordinal,
            ParentNavigationMenuItemId = entity.ParentNavigationMenuItemId,
            NavigationMenuId = entity.NavigationMenuId,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModifierUserId = entity.LastModifierUserId,
            LastModificationTime = entity.LastModificationTime,
        };
    }
}