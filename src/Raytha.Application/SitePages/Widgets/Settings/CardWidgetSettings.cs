namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Card widget - a single card with image, title, description, and CTA.
/// </summary>
public class CardWidgetSettings
{
    /// <summary>
    /// Card title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Card description/content (HTML from TipTap).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Card image URL (optional).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Image alt text for accessibility.
    /// </summary>
    public string? ImageAlt { get; set; }

    /// <summary>
    /// Button/link text (optional).
    /// </summary>
    public string? ButtonText { get; set; }

    /// <summary>
    /// Button/link URL (optional).
    /// </summary>
    public string? ButtonUrl { get; set; }

    /// <summary>
    /// Button style - primary, secondary, outline-primary, link.
    /// </summary>
    public string ButtonStyle { get; set; } = "primary";

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }
}

