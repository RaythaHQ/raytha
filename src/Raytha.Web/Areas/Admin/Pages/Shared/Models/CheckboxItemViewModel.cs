namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Reusable view model for checkbox list items used across admin forms.
/// Represents a single selectable item with an identifier and label.
/// </summary>
public record CheckboxItemViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for this checkbox item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool Selected { get; set; }

    /// <summary>
    /// Gets or sets the display label for this checkbox item.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
