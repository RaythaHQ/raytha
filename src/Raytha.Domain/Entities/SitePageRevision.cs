using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Raytha.Domain.Entities;

/// <summary>
/// Stores a historical snapshot of a Site Page's published widgets.
/// Created each time a Site Page is published (when not already in draft).
/// </summary>
public class SitePageRevision : BaseAuditableEntity
{
    /// <summary>
    /// JSON snapshot of the published widgets at the time of this revision.
    /// </summary>
    public string? _PublishedWidgetsJson { get; set; }

    /// <summary>
    /// Foreign key to the parent Site Page.
    /// </summary>
    public Guid SitePageId { get; set; }

    /// <summary>
    /// Navigation property to the parent Site Page.
    /// </summary>
    public virtual SitePage? SitePage { get; set; }

    private Dictionary<string, List<SitePageWidget>>? _publishedWidgetsDeserialized;

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
}

