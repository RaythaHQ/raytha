namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Image + Text widget - image alongside text content.
/// </summary>
public class ImageTextWidgetSettings
{
    /// <summary>
    /// URL of the image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Alt text for the image.
    /// </summary>
    public string? ImageAlt { get; set; }

    /// <summary>
    /// Heading text.
    /// </summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>
    /// Body content (HTML).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Image position - left or right.
    /// </summary>
    public string ImagePosition { get; set; } = "left";

    /// <summary>
    /// Optional button text.
    /// </summary>
    public string? ButtonText { get; set; }

    /// <summary>
    /// Optional button URL.
    /// </summary>
    public string? ButtonUrl { get; set; }

    /// <summary>
    /// Button style - primary, secondary, outline-primary, etc.
    /// </summary>
    public string ButtonStyle { get; set; } = "primary";

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }
}

