namespace Raytha.Domain.Entities;

public class RaythaFunction : BaseAuditableEntity, IPassivable
{
    public required string Name { get; set; }
    public required string DeveloperName { get; set; }
    public required RaythaFunctionTriggerType TriggerType { get; set; }
    public required string Code { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Optional public path (for example /llms.txt) that runs this HTTP-trigger function.
    /// </summary>
    public Guid? RouteId { get; set; }
    public virtual Route? Route { get; set; }
    public virtual ICollection<RaythaFunctionRevision> Revisions { get; set; } =
        new List<RaythaFunctionRevision>();
}
