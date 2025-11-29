namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the WYSIWYG widget - rich text content block.
/// </summary>
public class WysiwygWidgetSettings
{
    /// <summary>
    /// HTML content from WYSIWYG editor.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Padding size - none, small, medium, large.
    /// </summary>
    public string Padding { get; set; } = "medium";
}

