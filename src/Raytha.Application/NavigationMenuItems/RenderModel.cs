using System.Linq.Expressions;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.NavigationMenuItems;

public record NavigationMenuItem_RenderModel : IInsertTemplateVariable
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Url { get; init; }
    public bool IsDisabled { get; init; }
    public bool OpenInNewTab { get; init; }
    public string? CssClassName { get; init; }
    public int Ordinal { get; init; }
    public bool IsFirstItem { get; init; }
    public bool IsLastItem { get; init; }
    public required IEnumerable<NavigationMenuItem_RenderModel> MenuItems { get; init; }

    public static NavigationMenuItem_RenderModel Empty() =>
        new()
        {
            Id = string.Empty,
            Label = string.Empty,
            Url = string.Empty,
            MenuItems = new List<NavigationMenuItem_RenderModel>(),
        };

    public static Expression<
        Func<
            NavigationMenuItemDto,
            IReadOnlyCollection<NavigationMenuItem_RenderModel>,
            int,
            NavigationMenuItem_RenderModel
        >
    > GetProjection()
    {
        return (entity, nestedMenuItems, menuItemsWithSameParentCount) =>
            GetProjection(entity, nestedMenuItems, menuItemsWithSameParentCount);
    }

    public static NavigationMenuItem_RenderModel GetProjection(
        NavigationMenuItemDto entity,
        IReadOnlyCollection<NavigationMenuItem_RenderModel> nestedMenuItems,
        int menuItemsWithSameParentCount
    )
    {
        return new NavigationMenuItem_RenderModel
        {
            Id = entity.Id,
            Label = entity.Label,
            Url = entity.Url,
            IsDisabled = entity.IsDisabled,
            OpenInNewTab = entity.OpenInNewTab,
            CssClassName = entity.CssClassName,
            Ordinal = entity.Ordinal,
            IsFirstItem = entity.Ordinal == 1,
            IsLastItem = entity.Ordinal == menuItemsWithSameParentCount,
            MenuItems = nestedMenuItems,
        };
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(Label);
        yield return nameof(Url);
        yield return nameof(IsDisabled);
        yield return nameof(OpenInNewTab);
        yield return nameof(CssClassName);
        yield return nameof(Ordinal);
        yield return nameof(IsFirstItem);
        yield return nameof(IsLastItem);
        yield return nameof(MenuItems);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(
                developerName,
                $"MenuItem.{developerName}"
            );
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
