namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the FAQ widget - expandable accordion of questions and answers.
/// </summary>
public class FaqWidgetSettings
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
    /// Array of FAQ items.
    /// </summary>
    public List<FaqItem> Items { get; set; } = new List<FaqItem>();

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Whether to expand the first item by default.
    /// </summary>
    public bool ExpandFirst { get; set; } = true;

    /// <summary>
    /// Represents a single FAQ item.
    /// </summary>
    public class FaqItem
    {
        /// <summary>
        /// The question text.
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The answer text (HTML).
        /// </summary>
        public string Answer { get; set; } = string.Empty;
    }
}

