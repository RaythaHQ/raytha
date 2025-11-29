namespace Raytha.Domain.Entities;

/// <summary>
/// Revision history for WidgetTemplate changes.
/// </summary>
public class WidgetTemplateRevision : BaseAuditableEntity
{
    public string? Label { get; set; }
    public string? Content { get; set; }
    public Guid WidgetTemplateId { get; set; }
    public virtual WidgetTemplate? WidgetTemplate { get; set; }
}

