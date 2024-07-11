namespace Raytha.Domain.Entities;

public class ThemeAccessToMediaItem : BaseEntity
{
    public required Guid ThemeId { get; set; }
    public virtual Theme? Theme { get; set; }
    public required Guid MediaItemId { get; set; }
    public virtual MediaItem? MediaItem { get; set; }
}