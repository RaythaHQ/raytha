namespace Raytha.Domain.Entities;

public class NavigationMenuItem : BaseAuditableEntity
{
    public required string Label { get; set; }
    public required string Url { get; set; }
    public bool IsDisabled { get; set; }
    public bool OpenInNewTab { get; set; }
    public string? CssClassName { get; set; }
    public int Ordinal { get; set; }
    public Guid? ParentNavigationMenuItemId { get; set; }
    public virtual NavigationMenuItem? ParentNavigationMenuItem { get; set; }
    public required Guid NavigationMenuId { get; set; }
    public virtual NavigationMenu? NavigationMenu { get; set; }
}
