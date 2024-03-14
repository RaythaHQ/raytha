namespace Raytha.Domain.Entities;

public class RaythaFunctionRevision : BaseAuditableEntity
{
    public required string Code { get; set; }
    public Guid RaythaFunctionId { get; set; }
    public virtual RaythaFunction? RaythaFunction { get; set; }
}