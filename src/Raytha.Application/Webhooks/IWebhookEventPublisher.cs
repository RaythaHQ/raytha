namespace Raytha.Application.Webhooks;

/// <summary>
/// Fans an event out to every active, subscribed webhook by creating one delivery row
/// per target and enqueueing a background delivery task. Publishing must never fail
/// the business operation that raised the event.
/// </summary>
public interface IWebhookEventPublisher
{
    Task PublishAsync(string eventName, object? payload, CancellationToken cancellationToken);

    /// <summary>Creates and enqueues a single delivery to one webhook regardless of subscriptions.</summary>
    Task<Guid> PublishToAsync(
        Guid webhookId,
        string eventName,
        object? payload,
        CancellationToken cancellationToken
    );
}
