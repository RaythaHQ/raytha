namespace Raytha.Domain.Events;

public class ContentItemUpdatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public ContentItem ContentItem { get; private set; }

    public ContentItemUpdatedEvent(ContentItem contentItem)
    {
        ContentItem = contentItem;
    }
}
