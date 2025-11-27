namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Content List widget - displays a list of content items.
/// </summary>
public class ContentListWidgetSettings
{
    /// <summary>
    /// Section heading (optional).
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// Section subheading (optional).
    /// </summary>
    public string? Subheadline { get; set; }

    /// <summary>
    /// Developer name of content type to query.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// OData filter expression (optional).
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Order by expression (default: CreationTime desc).
    /// </summary>
    public string OrderBy { get; set; } = "CreationTime desc";

    /// <summary>
    /// Number of items to display.
    /// </summary>
    public int PageSize { get; set; } = 6;

    /// <summary>
    /// Display style - cards, list, compact.
    /// </summary>
    public string DisplayStyle { get; set; } = "cards";

    /// <summary>
    /// Number of columns for card view - 2, 3, 4.
    /// </summary>
    public int Columns { get; set; } = 3;

    /// <summary>
    /// Show featured image if available.
    /// </summary>
    public bool ShowImage { get; set; } = true;

    /// <summary>
    /// Show creation date.
    /// </summary>
    public bool ShowDate { get; set; } = true;

    /// <summary>
    /// Show content excerpt.
    /// </summary>
    public bool ShowExcerpt { get; set; } = true;

    /// <summary>
    /// Text for "View all" link (optional).
    /// </summary>
    public string? LinkText { get; set; }

    /// <summary>
    /// URL for "View all" link (optional).
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }
}

