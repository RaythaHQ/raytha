using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Raytha.Domain.Entities;

public class ContentItem : BaseAuditableEntity
{
    public bool IsPublished { get; set; }
    public bool IsDraft { get; set; }
    public string? _DraftContent { get; set; }
    public string? _PublishedContent { get; set; }
    public Guid ContentTypeId { get; set; }
    public virtual ContentType? ContentType { get; set; }
    public virtual ICollection<ContentItemRevision> ContentItemRevisions { get; set; } = new List<ContentItemRevision>();

    public Guid RouteId { get; set; }
    public Route Route { get; set; }

    private dynamic _draftContentAsDynamic;
    private dynamic _publishedContentAsDynamic;

    [NotMapped]
    public dynamic DraftContent 
    { 
        get 
        { 
            if (_draftContentAsDynamic == null)
            {
                _draftContentAsDynamic = JsonSerializer.Deserialize<dynamic>(_DraftContent ?? "{}");
            }
            return _draftContentAsDynamic; 
        }
        set 
        {
            _draftContentAsDynamic = value;
            _DraftContent = JsonSerializer.Serialize(value);
        }
    }

    [NotMapped]
    public dynamic PublishedContent 
    { 
        get 
        { 
            if (_publishedContentAsDynamic == null)
            {
                _publishedContentAsDynamic = JsonSerializer.Deserialize<dynamic>(_PublishedContent ?? "{}");
            }
            return _publishedContentAsDynamic; 
        } 
        set
        {
            _publishedContentAsDynamic = value;
            _PublishedContent = JsonSerializer.Serialize(value);
        }
    }

    [NotMapped]
    public string PrimaryField { get; set; } = string.Empty;

    public override string ToString()
    {
        return PrimaryField;
    }
}