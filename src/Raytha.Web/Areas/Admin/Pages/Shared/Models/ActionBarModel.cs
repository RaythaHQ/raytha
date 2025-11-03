#nullable enable
using System.Collections.Generic;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Model for rendering an action bar with buttons and optional search.
/// </summary>
public record ActionBarModel
{
    /// <summary>
    /// The list of buttons to display in the action bar.
    /// </summary>
    public required List<ActionBarButton> Buttons { get; init; }

    /// <summary>
    /// The search configuration, or null if search is not enabled.
    /// </summary>
    public SearchBarConfig? Search { get; init; }
}

/// <summary>
/// Represents a button in the action bar.
/// </summary>
public record ActionBarButton
{
    /// <summary>
    /// The label text for the button.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The SVG icon content (optional).
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The route/page path to navigate to.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// The route values dictionary (e.g., contentTypeDeveloperName).
    /// </summary>
    public Dictionary<string, string>? RouteValues { get; init; }

    /// <summary>
    /// The CSS class for the button (default: "btn btn-primary").
    /// </summary>
    public string CssClass { get; init; } = "btn btn-primary";
}

/// <summary>
/// Configuration for the search bar within an action bar.
/// </summary>
public record SearchBarConfig
{
    /// <summary>
    /// The page/action to submit the search form to.
    /// </summary>
    public required string ActionPage { get; init; }

    /// <summary>
    /// The current search query value.
    /// </summary>
    public string? SearchValue { get; init; }

    /// <summary>
    /// The placeholder text for the search input.
    /// </summary>
    public string Placeholder { get; init; } = "Search";
}
