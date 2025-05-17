namespace Raytha.Domain.Entities;

public record RecentlyAccessedView
{
    public Guid ContentTypeId { get; init; }
    public Guid ViewId { get; init; }
}
