namespace Raytha.Domain.Entities;

public class ContentType : BaseFullAuditableEntity, IPassivable
{
    public bool IsActive { get; set; } = true;
    public string? LabelPlural { get; set; }
    public string? LabelSingular { get; set; }
    public string? DeveloperName { get; set; }
    public string? Description { get; set; }
    public string? DefaultRouteTemplate { get; set; }
    public Guid PrimaryFieldId { get; set; }
    public virtual ICollection<ContentTypeField> ContentTypeFields { get; set; } =
        new List<ContentTypeField>();
    public virtual ICollection<View> Views { get; set; } = new List<View>();
}
