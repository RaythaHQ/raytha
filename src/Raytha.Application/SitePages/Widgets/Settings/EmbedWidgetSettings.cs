namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Embed widget - embed external content via iframe or HTML.
/// </summary>
public class EmbedWidgetSettings
{
    /// <summary>
    /// Type of embed - iframe, html.
    /// </summary>
    public string EmbedType { get; set; } = "iframe";

    /// <summary>
    /// URL for iframe embed.
    /// </summary>
    public string? IframeUrl { get; set; }

    /// <summary>
    /// Raw HTML/script content (use with caution).
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Aspect ratio - 16x9, 4x3, 1x1, 21x9.
    /// </summary>
    public string AspectRatio { get; set; } = "16x9";

    /// <summary>
    /// Maximum width in pixels (optional).
    /// </summary>
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Optional caption text below embed.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }
}

