namespace Raytha.Domain.Entities;

public class EmailTemplateRevision : BaseAuditableEntity
{
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public Guid EmailTemplateId { get; set; }
    public virtual EmailTemplate? EmailTemplate { get; set; }
}
