namespace Raytha.Domain.Entities;

public class NavigationMenu : BaseAuditableEntity
{
    public required string Label { get; set; }
    public required string DeveloperName { get; set; }
    public bool IsMainMenu { get; set; }
    public virtual ICollection<NavigationMenuItem> NavigationMenuItems { get; set; } =
        new List<NavigationMenuItem>();
    public virtual ICollection<NavigationMenuRevision> NavigationMenuRevisions { get; set; } =
        new List<NavigationMenuRevision>();
}
