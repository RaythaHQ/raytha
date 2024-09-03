namespace Raytha.Domain.Entities;

public class Theme : BaseAuditableEntity
{
    public const string DEFAULT_THEME_DEVELOPER_NAME = "raytha_default_theme";

    public required string Title { get; set; }
    public required string DeveloperName { get; set; }
    public required string Description { get; set; }
    public bool IsExportable { get; set; }
    public virtual ICollection<ThemeAccessToMediaItem> ThemeAccessToMediaItems { get; set; } = new List<ThemeAccessToMediaItem>();
    public virtual ICollection<WebTemplate> WebTemplates { get; set; } = new List<WebTemplate>();
}