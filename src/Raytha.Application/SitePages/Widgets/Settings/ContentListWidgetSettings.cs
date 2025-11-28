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
    /// View ID to use for filtering/ordering (optional).
    /// </summary>
    public string? ViewId { get; set; }

    /// <summary>
    /// Additional OData filter expression (optional).
    /// </summary>
    public string? Filter { get; set; } = "IsPublished eq 'true'";

    /// <summary>
    /// Order by expression - OData format (optional).
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Number of items to display.
    /// </summary>
    public int PageSize { get; set; } = 3;

    /// <summary>
    /// Display style - cards, list, compact.
    /// </summary>
    public string DisplayStyle { get; set; } = "cards";

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

