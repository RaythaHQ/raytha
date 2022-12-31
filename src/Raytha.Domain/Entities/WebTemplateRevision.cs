namespace Raytha.Domain.Entities;

public class WebTemplateRevision : BaseAuditableEntity
{
    public string? Label { get; set; }
    public string? Content { get; set; }
    public Guid WebTemplateId { get; set; }
    public virtual WebTemplate? WebTemplate { get; set; }
    public bool AllowAccessForNewContentTypes { get; set; }
}
