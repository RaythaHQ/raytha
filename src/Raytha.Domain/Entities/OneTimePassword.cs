namespace Raytha.Domain.Entities;

public class OneTimePassword : BaseEntity, IHasCreationTime
{
    public new byte[]? Id { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public Guid UserId { get; set; }
    public virtual User? User { get; set; }
}