using CSharpVitamins;
using Raytha.Application.NavigationMenuItems;

namespace Raytha.Application.Common.Utils;

public static class NavigationMenuItemExtensions
{
    public static IReadOnlyCollection<TModel> BuildTree<TModel>(
        this IReadOnlyCollection<NavigationMenuItemDto> navigationMenuItems,
        Func<NavigationMenuItemDto, IReadOnlyCollection<TModel>, int, TModel> createModel,
        ShortGuid? parentNavigationMenuItemId = null
    )
    {
        var filteredNavigationMenuItemsByParent = navigationMenuItems
            .Where(nmi => nmi.ParentNavigationMenuItemId == parentNavigationMenuItemId)
            .OrderBy(nmi => nmi.Ordinal)
            .ToList();

        return filteredNavigationMenuItemsByParent
            .Select(nmi =>
                createModel(
                    nmi,
                    navigationMenuItems.BuildTree(createModel, nmi.Id),
                    filteredNavigationMenuItemsByParent.Count
                )
            )
            .ToList();
    }

    public static IReadOnlyCollection<NavigationMenuItemDto> ExcludeNestedNavigationMenuItems(
        this IReadOnlyCollection<NavigationMenuItemDto> navigationMenuItems,
        ShortGuid id
    )
    {
        var nestedNavigationMenuItemIds = navigationMenuItems.GetNestedNavigationMenuItemIds(id);

        return navigationMenuItems
            .Where(nmi => !nestedNavigationMenuItemIds.Contains(nmi.Id))
            .ToList();
    }

    public static IReadOnlyCollection<ShortGuid> GetNestedNavigationMenuItemIds(
        this IReadOnlyCollection<NavigationMenuItemDto> navigationMenuItems,
        ShortGuid id
    )
    {
        var nestedNavigationMenuItemIds = navigationMenuItems
            .Where(nmi => nmi.ParentNavigationMenuItemId == id)
            .Select(nmi => nmi.Id)
            .ToList();

        return nestedNavigationMenuItemIds
            .Concat(
                nestedNavigationMenuItemIds.SelectMany(
                    navigationMenuItems.GetNestedNavigationMenuItemIds
                )
            )
            .ToArray();
    }
}
