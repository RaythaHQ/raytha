namespace Raytha.Domain.Entities;

public class JwtLogin : BaseEntity, IHasCreationTime
{
    public string? Jti { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}