#nullable enable

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Model for rendering an empty state component.
/// </summary>
public record EmptyStateModel
{
    /// <summary>
    /// The title text to display.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The optional message text to display below the title.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// The SVG icon content (optional).
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The optional action button configuration.
    /// </summary>
    public EmptyStateAction? Action { get; init; }
}

/// <summary>
/// Represents an action button in an empty state.
/// </summary>
public record EmptyStateAction
{
    /// <summary>
    /// The label text for the action button.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The route/page path to navigate to.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// The route values dictionary (optional).
    /// </summary>
    public System.Collections.Generic.Dictionary<string, string>? RouteValues { get; init; }
}
