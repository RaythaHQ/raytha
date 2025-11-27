using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Raytha.Domain.Entities;

/// <summary>
/// Represents a Site Page - a first-class "one-off page" (home, about, landing pages)
/// that uses the widget/layout system. Distinct from ContentItem/ContentType.
/// </summary>
public class SitePage : BaseAuditableEntity
{
    /// <summary>
    /// The page title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Whether the page is published and visible to the public.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Whether the page has unpublished draft changes.
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Foreign key to the Route table for URL path.
    /// </summary>
    public Guid RouteId { get; set; }

    /// <summary>
    /// Navigation property to the Route.
    /// </summary>
    public virtual Route? Route { get; set; }

    /// <summary>
    /// Foreign key to the WebTemplate used to render this page.
    /// </summary>
    public Guid WebTemplateId { get; set; }

    /// <summary>
    /// Navigation property to the WebTemplate.
    /// </summary>
    public virtual WebTemplate? WebTemplate { get; set; }

    /// <summary>
    /// JSON-serialized dictionary of widgets by section name.
    /// Key = section developer name (e.g., "hero", "main", "sidebar")
    /// Value = ordered list of widgets for that section
    /// </summary>
    public string _WidgetsJson { get; set; } = "{}";

    private Dictionary<string, List<SitePageWidget>>? _widgetsDeserialized;

    /// <summary>
    /// Dictionary of widgets organized by section name.
    /// Order of widgets in each list determines render order.
    /// </summary>
    [NotMapped]
    public Dictionary<string, List<SitePageWidget>> Widgets
    {
        get
        {
            if (_widgetsDeserialized == null)
            {
                _widgetsDeserialized =
                    JsonSerializer.Deserialize<Dictionary<string, List<SitePageWidget>>>(
                        _WidgetsJson ?? "{}"
                    ) ?? new Dictionary<string, List<SitePageWidget>>();
            }
            return _widgetsDeserialized;
        }
        set
        {
            _widgetsDeserialized = value;
            _WidgetsJson = JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Gets the widgets for a specific section.
    /// </summary>
    /// <param name="sectionName">The section developer name.</param>
    /// <returns>List of widgets for the section, or empty list if section doesn't exist.</returns>
    public List<SitePageWidget> GetWidgetsForSection(string sectionName)
    {
        return Widgets.TryGetValue(sectionName, out var widgets)
            ? widgets
            : new List<SitePageWidget>();
    }

    public override string ToString()
    {
        return Title;
    }
}
