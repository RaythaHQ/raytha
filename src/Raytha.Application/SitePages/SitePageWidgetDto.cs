using CSharpVitamins;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages;

/// <summary>
/// DTO for a widget instance within a SitePage section.
/// </summary>
public record SitePageWidgetDto
{
    /// <summary>
    /// Unique identifier for this widget instance.
    /// </summary>
    public ShortGuid Id { get; init; }

    /// <summary>
    /// The widget type identifier (e.g., "hero", "wysiwyg", "cta").
    /// Maps to BuiltInWidgetType.DeveloperName.
    /// </summary>
    public string WidgetType { get; init; } = string.Empty;

    /// <summary>
    /// JSON-serialized widget settings specific to the widget type.
    /// </summary>
    public string SettingsJson { get; init; } = "{}";

    /// <summary>
    /// Row index for grid layout (0-indexed).
    /// Widgets with the same Row are rendered in the same Bootstrap row.
    /// </summary>
    public int Row { get; init; }

    /// <summary>
    /// Column start position within the row (0-11 for 12-column grid).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Number of columns this widget spans (1-12).
    /// Maps to Bootstrap col-md-{ColumnSpan}.
    /// </summary>
    public int ColumnSpan { get; init; } = 12;

    public static SitePageWidgetDto GetProjection(SitePageWidget entity)
    {
        if (entity == null)
            return null!;

        return new SitePageWidgetDto
        {
            Id = entity.Id,
            WidgetType = entity.WidgetType,
            SettingsJson = entity.SettingsJson,
            Row = entity.Row,
            Column = entity.Column,
            ColumnSpan = entity.ColumnSpan,
        };
    }

    /// <summary>
    /// Converts this DTO back to a domain entity (for saving).
    /// </summary>
    public SitePageWidget ToEntity()
    {
        return new SitePageWidget
        {
            Id = Id.Guid,
            WidgetType = WidgetType,
            SettingsJson = SettingsJson,
            Row = Row,
            Column = Column,
            ColumnSpan = ColumnSpan,
        };
    }
}
