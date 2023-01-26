namespace Raytha.Domain.Events;

public class ContentItemDeletedEvent : BaseEvent, IBeforeSaveChangesNotification
{
    public ContentItem ContentItem { get; private set; }

    public ContentItemDeletedEvent(ContentItem contentItem)
    {
        ContentItem = contentItem;
    }
}
