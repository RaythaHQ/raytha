using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Text.Json;

namespace Raytha.Domain.Entities;

public class ContentItemRevision : BaseAuditableEntity
{
    public string? _PublishedContent { get; set; }
    public Guid ContentItemId { get; set; }
    public virtual ContentItem? ContentItem { get; set; }

    [NotMapped]
    public dynamic PublishedContent
    {
        get { return JsonSerializer.Deserialize<dynamic>(_PublishedContent ?? "[]") ?? new ExpandoObject(); }
        set { _PublishedContent = JsonSerializer.Serialize(value); }
    }
}