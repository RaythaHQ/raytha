namespace Raytha.Domain.Entities;

public class Route : BaseEntity
{
    public const string CONTENT_ITEM_TYPE = "ContentItem";
    public const string VIEW_TYPE = "View";
    public const string UNKNOWN_TYPE = "Unknown";

    public string Path { get; set; }

    public Guid ContentItemId { get; set; }
    public virtual ContentItem ContentItem { get; set; }

    public Guid ViewId { get; set; }
    public virtual View View { get; set; }
}
