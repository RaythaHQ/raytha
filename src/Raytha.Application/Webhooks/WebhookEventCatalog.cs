using System.Reflection;

namespace Raytha.Application.Webhooks;

public record WebhookEventDescriptor(string EventName, string DisplayName, string Group);

public record WebhookEventGroup(string Group, IReadOnlyList<WebhookEventDescriptor> Events);

public interface IWebhookEventCatalog
{
    IReadOnlyList<WebhookEventDescriptor> Events { get; }
    IReadOnlyList<WebhookEventGroup> Groups { get; }
    bool IsKnownEvent(string eventName);
}

/// <summary>
/// Reflects over the Application assembly once for types carrying
/// <see cref="WebhookEventAttribute"/>. Nested <c>Command</c> records inherit the
/// attribute from their declaring type when the attribute lives on the outer class.
/// </summary>
public sealed class WebhookEventCatalog : IWebhookEventCatalog
{
    public WebhookEventCatalog()
        : this(new[] { typeof(WebhookEventCatalog).Assembly }) { }

    public WebhookEventCatalog(IEnumerable<Assembly> assemblies)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var events = new List<WebhookEventDescriptor>();

        foreach (var assembly in assemblies.Distinct())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<WebhookEventAttribute>();
                if (attr is null || !seen.Add(attr.EventName))
                {
                    continue;
                }

                events.Add(
                    new WebhookEventDescriptor(
                        attr.EventName,
                        attr.DisplayName ?? attr.EventName,
                        attr.Group
                    )
                );
            }
        }

        Events = events
            .OrderBy(e => e.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Groups = Events
            .GroupBy(e => e.Group, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new WebhookEventGroup(g.Key, g.ToList()))
            .ToList();
    }

    public IReadOnlyList<WebhookEventDescriptor> Events { get; }

    public IReadOnlyList<WebhookEventGroup> Groups { get; }

    public bool IsKnownEvent(string eventName)
    {
        return eventName == Domain.Entities.Webhook.SubscribeToAll
            || Events.Any(e =>
                string.Equals(e.EventName, eventName, StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <summary>Resolves the attribute for a Mediator message type (checks the type, then its declaring type).</summary>
    public static WebhookEventAttribute? FindAttribute(Type messageType)
    {
        return messageType.GetCustomAttribute<WebhookEventAttribute>()
            ?? messageType.DeclaringType?.GetCustomAttribute<WebhookEventAttribute>();
    }
}
