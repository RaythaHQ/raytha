using System.Linq.Expressions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.NavigationMenuItems;

namespace Raytha.Application.NavigationMenus;

public record NavigationMenu_RenderModel : IInsertTemplateVariable
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string DeveloperName { get; init; }
    public bool IsMainMenu { get; init; }
    public required IEnumerable<NavigationMenuItem_RenderModel> MenuItems { get; init; }

    public static NavigationMenu_RenderModel Empty() =>
        new()
        {
            Id = string.Empty,
            Label = string.Empty,
            DeveloperName = string.Empty,
            MenuItems = new List<NavigationMenuItem_RenderModel>(),
        };

    public static Expression<
        Func<
            NavigationMenuDto,
            IReadOnlyCollection<NavigationMenuItem_RenderModel>,
            NavigationMenu_RenderModel
        >
    > GetProjection()
    {
        return (entity, menuItems) => GetProjection(entity, menuItems);
    }

    public static NavigationMenu_RenderModel GetProjection(
        NavigationMenuDto entity,
        IReadOnlyCollection<NavigationMenuItem_RenderModel> menuItems
    )
    {
        return new NavigationMenu_RenderModel
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            IsMainMenu = entity.IsMainMenu,
            MenuItems = menuItems,
        };
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(Label);
        yield return nameof(DeveloperName);
        yield return nameof(IsMainMenu);
        yield return nameof(MenuItems);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"Menu.{developerName}");
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
