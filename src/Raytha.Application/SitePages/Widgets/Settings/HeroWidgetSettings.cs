namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Hero widget - large banner with headline, subtext, and CTA.
/// </summary>
public class HeroWidgetSettings
{
    /// <summary>
    /// Main heading text.
    /// </summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>
    /// Secondary text below headline.
    /// </summary>
    public string Subheadline { get; set; } = string.Empty;

    /// <summary>
    /// URL for background image (optional).
    /// </summary>
    public string? BackgroundImage { get; set; }

    /// <summary>
    /// Background color hex code (default: #0d6efd).
    /// </summary>
    public string BackgroundColor { get; set; } = "#0d6efd";

    /// <summary>
    /// Text color hex code (default: #ffffff).
    /// </summary>
    public string TextColor { get; set; } = "#ffffff";

    /// <summary>
    /// CTA button text (optional).
    /// </summary>
    public string? ButtonText { get; set; }

    /// <summary>
    /// CTA button link URL (optional).
    /// </summary>
    public string? ButtonUrl { get; set; }

    /// <summary>
    /// Button style - primary, secondary, outline-light, light, etc.
    /// </summary>
    public string ButtonStyle { get; set; } = "light";

    /// <summary>
    /// Text alignment - left, center, right.
    /// </summary>
    public string Alignment { get; set; } = "center";

    /// <summary>
    /// Minimum height in pixels.
    /// </summary>
    public int MinHeight { get; set; } = 400;
}

