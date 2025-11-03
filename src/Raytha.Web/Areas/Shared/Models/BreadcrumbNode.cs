#nullable enable
namespace Raytha.Web.Areas.Shared.Models;

/// <summary>
/// Represents a single breadcrumb node in the navigation trail.
/// </summary>
public record BreadcrumbNode
{
    /// <summary>
    /// Gets or sets the display label for the breadcrumb.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Gets or sets the route name to navigate to when clicked.
    /// If null, the breadcrumb will not be a link (e.g., for the current page).
    /// </summary>
    public string? RouteName { get; init; }

    /// <summary>
    /// Gets or sets the route values (parameters) for the route.
    /// </summary>
    public Dictionary<string, string>? RouteValues { get; init; }

    /// <summary>
    /// Gets or sets whether this is the active/current breadcrumb.
    /// Active breadcrumbs are typically not clickable and styled differently.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the icon SVG markup to display before the label (optional).
    /// </summary>
    public string? Icon { get; init; }
}
