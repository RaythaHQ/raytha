namespace Raytha.Application.SitePages.Widgets.Settings;

/// <summary>
/// Settings for the Card Grid widget - grid of cards with images and content.
/// </summary>
public class CardGridWidgetSettings
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
    /// Number of columns - 2, 3, 4.
    /// </summary>
    public int Columns { get; set; } = 3;

    /// <summary>
    /// Array of card objects.
    /// </summary>
    public List<CardItem> Cards { get; set; } = new List<CardItem>();

    /// <summary>
    /// Background color (optional).
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Represents a single card in the grid.
    /// </summary>
    public class CardItem
    {
        /// <summary>
        /// Card image URL.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Card title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Card description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Link URL (optional).
        /// </summary>
        public string? LinkUrl { get; set; }

        /// <summary>
        /// Link text (default: "Learn more").
        /// </summary>
        public string LinkText { get; set; } = "Learn more";
    }
}

