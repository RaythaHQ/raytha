namespace Raytha.Domain.Entities;

public class AuditLog : BaseEntity, IHasCreationTime
{
    public Guid? EntityId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Request { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}
