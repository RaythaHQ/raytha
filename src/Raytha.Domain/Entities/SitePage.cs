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
    /// JSON-serialized dictionary of draft widgets by section name.
    /// This stores the current working version (edits before publishing).
    /// </summary>
    public string? _DraftWidgetsJson { get; set; }

    /// <summary>
    /// JSON-serialized dictionary of published widgets by section name.
    /// This stores the live/published version shown to visitors.
    /// </summary>
    public string? _PublishedWidgetsJson { get; set; }

    private Dictionary<string, List<SitePageWidget>>? _draftWidgetsDeserialized;
    private Dictionary<string, List<SitePageWidget>>? _publishedWidgetsDeserialized;

    /// <summary>
    /// Dictionary of draft widgets organized by section name.
    /// </summary>
    [NotMapped]
    public Dictionary<string, List<SitePageWidget>> DraftWidgets
    {
        get
        {
            if (_draftWidgetsDeserialized == null)
            {
                _draftWidgetsDeserialized =
                    JsonSerializer.Deserialize<Dictionary<string, List<SitePageWidget>>>(
                        _DraftWidgetsJson ?? "{}"
                    ) ?? new Dictionary<string, List<SitePageWidget>>();
            }
            return _draftWidgetsDeserialized;
        }
        set
        {
            _draftWidgetsDeserialized = value;
            _DraftWidgetsJson = JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Dictionary of published widgets organized by section name.
    /// </summary>
    [NotMapped]
    public Dictionary<string, List<SitePageWidget>> PublishedWidgets
    {
        get
        {
            if (_publishedWidgetsDeserialized == null)
            {
                _publishedWidgetsDeserialized =
                    JsonSerializer.Deserialize<Dictionary<string, List<SitePageWidget>>>(
                        _PublishedWidgetsJson ?? "{}"
                    ) ?? new Dictionary<string, List<SitePageWidget>>();
            }
            return _publishedWidgetsDeserialized;
        }
        set
        {
            _publishedWidgetsDeserialized = value;
            _PublishedWidgetsJson = JsonSerializer.Serialize(value);
        }
    }

    /// <summary>
    /// Gets the current working widgets - draft if available, otherwise published.
    /// Use this for editing in the admin UI.
    /// </summary>
    [NotMapped]
    public Dictionary<string, List<SitePageWidget>> Widgets
    {
        get => IsDraft ? DraftWidgets : PublishedWidgets;
        set
        {
            if (IsDraft)
            {
                DraftWidgets = value;
            }
            else
            {
                PublishedWidgets = value;
            }
        }
    }

    /// <summary>
    /// Gets the widgets for a specific section from the published version.
    /// Use this for public rendering.
    /// </summary>
    public List<SitePageWidget> GetPublishedWidgetsForSection(string sectionName)
    {
        return PublishedWidgets.TryGetValue(sectionName, out var widgets)
            ? widgets
            : new List<SitePageWidget>();
    }

    /// <summary>
    /// Gets the widgets for a specific section from the current working version.
    /// Use this for admin preview/editing.
    /// </summary>
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
