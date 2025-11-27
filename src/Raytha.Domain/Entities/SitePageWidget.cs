namespace Raytha.Domain.Entities;

/// <summary>
/// Represents a widget instance within a SitePage section.
/// This is a serializable class stored as JSON within SitePage, NOT a database entity.
/// </summary>
public class SitePageWidget
{
    /// <summary>
    /// Unique identifier for this widget instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The widget type identifier (e.g., "hero", "richtext", "cta").
    /// Maps to BuiltInWidgetType.SystemName.
    /// </summary>
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized widget settings specific to the widget type.
    /// </summary>
    public string SettingsJson { get; set; } = "{}";

    /// <summary>
    /// Row index for grid layout (0-indexed).
    /// Widgets with the same Row are rendered in the same Bootstrap row.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Column start position within the row (0-11 for 12-column grid).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Number of columns this widget spans (1-12).
    /// Maps to Bootstrap col-md-{ColumnSpan}.
    /// </summary>
    public int ColumnSpan { get; set; } = 12;
}
