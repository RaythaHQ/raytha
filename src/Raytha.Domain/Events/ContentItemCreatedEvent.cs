namespace Raytha.Domain.Events;

public class ContentItemCreatedEvent : BaseEvent, IAfterSaveChangesNotification
{
    public ContentItem ContentItem { get; private set; }

    public ContentItemCreatedEvent(ContentItem contentItem)
    {
        ContentItem = contentItem;
    }
}
