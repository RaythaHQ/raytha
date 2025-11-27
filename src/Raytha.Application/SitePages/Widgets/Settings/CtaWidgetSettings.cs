namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the CTA widget - prominent call to action block.
/// </summary>
public class CtaWidgetSettings
{
    /// <summary>
    /// Main heading.
    /// </summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>
    /// Supporting text (HTML).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Button label.
    /// </summary>
    public string? ButtonText { get; set; }

    /// <summary>
    /// Button link URL.
    /// </summary>
    public string? ButtonUrl { get; set; }

    /// <summary>
    /// Button style - primary, secondary, light, etc.
    /// </summary>
    public string ButtonStyle { get; set; } = "light";

    /// <summary>
    /// Background color (default: #0d6efd).
    /// </summary>
    public string BackgroundColor { get; set; } = "#0d6efd";

    /// <summary>
    /// Text color (default: #ffffff).
    /// </summary>
    public string TextColor { get; set; } = "#ffffff";

    /// <summary>
    /// Text alignment - left, center, right.
    /// </summary>
    public string Alignment { get; set; } = "center";
}

