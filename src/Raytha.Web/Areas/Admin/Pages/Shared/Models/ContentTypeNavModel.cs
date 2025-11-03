#nullable enable
using Raytha.Application.Views;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Model for rendering the content type navigation tabs.
/// </summary>
public record ContentTypeNavModel
{
    /// <summary>
    /// The current view with content type information.
    /// </summary>
    public required ViewDto CurrentView { get; init; }

    /// <summary>
    /// The active tab to highlight in the navigation.
    /// </summary>
    public required ContentTypeNavTab ActiveTab { get; init; }
}

/// <summary>
/// Enum representing the available navigation tabs for content type pages.
/// </summary>
public enum ContentTypeNavTab
{
    /// <summary>
    /// Content items list page.
    /// </summary>
    ContentItems,

    /// <summary>
    /// Configuration page.
    /// </summary>
    Configuration,

    /// <summary>
    /// Fields management page.
    /// </summary>
    Fields,

    /// <summary>
    /// Deleted items page.
    /// </summary>
    DeletedItems,

    /// <summary>
    /// Views management page.
    /// </summary>
    Views,
}
