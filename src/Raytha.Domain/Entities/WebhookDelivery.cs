namespace Raytha.Domain.Entities;

/// <summary>
/// One event dispatched to one webhook: the payload, attempt bookkeeping, and the outcome.
/// Kept for observability and manual redelivery.
/// </summary>
public class WebhookDelivery : BaseEntity, IHasCreationTime
{
    public const int MaxResponseBodyLength = 2000;

    public Guid WebhookId { get; set; }
    public virtual Webhook Webhook { get; set; } = null!;

    public string EventName { get; set; } = string.Empty;

    /// <summary>The JSON body exactly as POSTed (the signature is computed over it).</summary>
    public string Payload { get; set; } = string.Empty;

    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;

    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }

    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }

    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? CompletionTime { get; set; }
}

public class WebhookDeliveryStatus : ValueObject
{
    static WebhookDeliveryStatus() { }

    public WebhookDeliveryStatus() { }

    private WebhookDeliveryStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static WebhookDeliveryStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p =>
            p.DeveloperName == developerName.ToLower()
        );

        if (type == null)
        {
            throw new ArgumentException(
                $"'{developerName}' is not a supported webhook delivery status.",
                nameof(developerName)
            );
        }

        return type;
    }

    public static WebhookDeliveryStatus Pending => new("Pending", "pending");
    public static WebhookDeliveryStatus Succeeded => new("Succeeded", "succeeded");
    public static WebhookDeliveryStatus Failed => new("Failed", "failed");

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(WebhookDeliveryStatus status)
    {
        return status.DeveloperName;
    }

    public static explicit operator WebhookDeliveryStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<WebhookDeliveryStatus> SupportedTypes
    {
        get
        {
            yield return Pending;
            yield return Succeeded;
            yield return Failed;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
