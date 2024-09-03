namespace Raytha.Domain.Entities;

public class WebTemplateContentItemRelation : BaseEntity
{
    public required Guid WebTemplateId { get; set; }
    public virtual WebTemplate? WebTemplate { get; set; }
    public required Guid ContentItemId { get; set; }
    public virtual ContentItem? ContentItem { get; set; }
}