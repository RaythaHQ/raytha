namespace Raytha.Domain.Entities;

public class RaythaFunction : BaseAuditableEntity, IPassivable
{
    public required string Name { get; set; }
    public required string DeveloperName { get; set; }
    public required RaythaFunctionTriggerType TriggerType { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }
    public virtual ICollection<RaythaFunctionRevision> Revisions { get; set; } = new List<RaythaFunctionRevision>();
}