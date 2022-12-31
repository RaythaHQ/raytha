namespace Raytha.Domain.Entities;

public class UserGroup : BaseAuditableEntity
{
    public string Label { get; set; } = null!;
    public string DeveloperName { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; }
}