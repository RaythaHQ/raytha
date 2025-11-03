#nullable enable
namespace Raytha.Web.Areas.Admin.Pages.Shared.Infrastructure.Navigation;

/// <summary>
/// Represents a navigation menu item with metadata for rendering and permissions.
/// </summary>
public class NavMenuItem
{
    /// <summary>
    /// Gets or sets the unique identifier for this menu item.
    /// Used for matching active state (ViewData["ActiveMenu"]).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the display label for the menu item.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets the route name for navigation.
    /// Should be a constant from RouteNames class.
    /// </summary>
    public string? RouteName { get; init; }

    /// <summary>
    /// Gets or sets the SVG icon markup to display.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets or sets the permission required to view this menu item.
    /// If null, no permission check is required.
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    /// Gets or sets the display order for sorting menu items.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets or sets the child menu items for nested navigation.
    /// </summary>
    public IEnumerable<NavMenuItem>? Children { get; init; }

    /// <summary>
    /// Gets or sets whether this menu item is a divider/separator.
    /// </summary>
    public bool IsDivider { get; init; }

    /// <summary>
    /// Gets or sets additional CSS classes to apply to the menu item.
    /// </summary>
    public string? CssClass { get; init; }

    /// <summary>
    /// Gets or sets whether the link should open in a new tab.
    /// </summary>
    public bool OpenInNewTab { get; init; }
}
