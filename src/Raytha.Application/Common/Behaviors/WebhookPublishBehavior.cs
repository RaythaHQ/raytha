using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mediator;
using Microsoft.Extensions.Logging;
using Raytha.Application.FeatureFlags;
using Raytha.Application.Webhooks;

namespace Raytha.Application.Common.Behaviors;

/// <summary>
/// After a successful command annotated with <see cref="WebhookEventAttribute"/>,
/// publishes the event to subscribed webhooks. Publish failures are logged and never
/// fail the command.
/// </summary>
public sealed class WebhookPublishBehavior<TMessage, TResponse>
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly WebhookEventAttribute? Attribute = WebhookEventCatalog.FindAttribute(
        typeof(TMessage)
    );

    private static readonly string[] SensitivePropertyFragments =
    {
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
    };

    private readonly IWebhookEventPublisher _publisher;
    private readonly IFeatureFlagService _featureFlags;
    private readonly ILogger<WebhookPublishBehavior<TMessage, TResponse>> _logger;

    public WebhookPublishBehavior(
        IWebhookEventPublisher publisher,
        IFeatureFlagService featureFlags,
        ILogger<WebhookPublishBehavior<TMessage, TResponse>> logger
    )
    {
        _publisher = publisher;
        _featureFlags = featureFlags;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var response = await next(message, cancellationToken);

        if (Attribute is null || !IsSuccessful(response))
        {
            return response;
        }

        try
        {
            if (
                !await _featureFlags.IsEnabledAsync(RaythaFeatureFlags.Webhooks, cancellationToken)
            )
            {
                return response;
            }

            var payload = BuildPayload(message, response);
            await _publisher.PublishAsync(Attribute.EventName, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Webhook publish failed for {EventName} after {Request}",
                Attribute.EventName,
                typeof(TMessage).Name
            );
        }

        return response;
    }

    private static bool IsSuccessful(TResponse? response)
    {
        if (response is null)
        {
            return false;
        }

        var successProperty = response
            .GetType()
            .GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
        if (successProperty is null || successProperty.PropertyType != typeof(bool))
        {
            return true;
        }

        return (bool)(successProperty.GetValue(response) ?? false);
    }

    private static object BuildPayload(TMessage message, TResponse response)
    {
        object? result = null;
        if (response is not null)
        {
            var resultProperty = response
                .GetType()
                .GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            result = resultProperty?.GetValue(response);
        }

        return new
        {
            result,
            request = Sanitize(message),
            requestType = typeof(TMessage).FullName?.Replace("Raytha.Application.", string.Empty),
        };
    }

    /// <summary>
    /// Serializes the request and strips any property whose name looks credential-bearing so
    /// webhook receivers never see plaintext passwords or secrets.
    /// </summary>
    public static JsonNode? Sanitize(object message)
    {
        JsonNode? node;
        try
        {
            node = JsonSerializer.SerializeToNode(
                message,
                message.GetType(),
                WebhookEventPublisher.PayloadJsonOptions
            );
        }
        catch (Exception)
        {
            return null;
        }

        Redact(node);
        return node;
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    var lowered = key.ToLowerInvariant();
                    if (SensitivePropertyFragments.Any(lowered.Contains))
                    {
                        obj.Remove(key);
                    }
                    else
                    {
                        Redact(obj[key]);
                    }
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    Redact(item);
                }
                break;
        }
    }
}
