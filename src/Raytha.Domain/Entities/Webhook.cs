namespace Raytha.Domain.Entities;

/// <summary>
/// An outbound webhook subscription. Deliveries are signed with <see cref="Secret"/>
/// (HMAC-SHA256, X-Raytha-Signature header) and retried with exponential backoff.
/// </summary>
public class Webhook : BaseAuditableEntity, IPassivable
{
    public const string SubscribeToAll = "*";

    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>HMAC-SHA256 secret used to sign payloads.</summary>
    public string Secret { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Event names this webhook subscribes to (e.g. "content_item.created").
    /// "*" subscribes to everything. Persisted as jsonb.
    /// </summary>
    public string[] SubscribedEvents { get; set; } = Array.Empty<string>();

    /// <summary>Total delivery attempts (first try + retries).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Per-attempt HTTP timeout.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public virtual ICollection<WebhookDelivery> Deliveries { get; set; } =
        new List<WebhookDelivery>();

    public bool SubscribesTo(string eventName)
    {
        return SubscribedEvents.Any(e =>
            e == SubscribeToAll || string.Equals(e, eventName, StringComparison.OrdinalIgnoreCase)
        );
    }
}
