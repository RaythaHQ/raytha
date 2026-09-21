namespace Raytha.Application.Webhooks;

/// <summary>
/// Marks a command as a webhook trigger. Place on the outer command class
/// (e.g. <c>CreateContentItem</c>) or on the nested <c>Command</c> record.
/// On a successful response the Mediator pipeline publishes <see cref="EventName"/>
/// to every active webhook subscribed to it.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WebhookEventAttribute : Attribute
{
    public WebhookEventAttribute(string eventName)
    {
        EventName = eventName;
    }

    /// <summary>Dot-namespaced event id, e.g. "content_item.created" or "user.created".</summary>
    public string EventName { get; }

    /// <summary>Human label shown in the webhook admin UI. Defaults to the event name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Grouping header in the webhook event picker, e.g. "Content" or "Users".</summary>
    public string Group { get; init; } = "General";
}
