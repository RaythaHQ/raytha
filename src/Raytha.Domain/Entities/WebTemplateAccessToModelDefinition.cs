namespace Raytha.Domain.Entities;

public class WebTemplateAccessToModelDefinition : BaseEntity
{
    public Guid WebTemplateId { get; set; }
    public virtual WebTemplate? WebTemplate { get; set; }
    public Guid ContentTypeId { get; set; }
    public virtual ContentType? ContentType { get; set; }
}
