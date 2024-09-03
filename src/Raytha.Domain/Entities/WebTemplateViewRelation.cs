namespace Raytha.Domain.Entities;

public class WebTemplateViewRelation : BaseEntity
{
    public required Guid WebTemplateId { get; set; }
    public virtual WebTemplate? WebTemplate { get; set; }
    public required Guid ViewId { get; set; }
    public virtual View? View { get; set; }
}