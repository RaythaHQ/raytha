namespace Raytha.Domain.Entities;

public class ApiKey : BaseEntity, ICreationAuditable
{
    public byte[] ApiKeyHash { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public Guid? CreatorUserId { get; set; }
    public virtual User? CreatorUser { get; set; }

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}
