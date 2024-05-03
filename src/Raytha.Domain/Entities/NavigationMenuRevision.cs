namespace Raytha.Domain.Entities;

public class NavigationMenuRevision : BaseAuditableEntity
{
    public required string NavigationMenuItemsJson { get; set; }
    public required Guid NavigationMenuId { get; set; }
    public virtual NavigationMenu? NavigationMenu { get; set; }
}