using System.Linq.Expressions;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenuItems;

public class NavigationMenuItemJson
{
    public required Guid Id { get; init; }
    public required string Label { get; init; }
    public required string Url { get; init; }
    public bool IsDisabled { get; init; }
    public bool OpenInNewTab { get; init; }
    public string? CssClassName { get; init; }
    public int Ordinal { get; init; }
    public Guid? ParentNavigationMenuItemId { get; init; }
    public required Guid NavigationMenuId { get; init; }

    public static Expression<Func<NavigationMenuItem, NavigationMenuItemJson>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static NavigationMenuItemJson GetProjection(NavigationMenuItem entity)
    {
        return new NavigationMenuItemJson
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
        };
    }
}