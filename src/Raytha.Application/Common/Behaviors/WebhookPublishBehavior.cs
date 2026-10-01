using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
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

    private const string Savepoint = "webhook_publish";

    private readonly IWebhookEventPublisher _publisher;
    private readonly IRaythaDbContext _db;
    private readonly ILogger<WebhookPublishBehavior<TMessage, TResponse>> _logger;

    public WebhookPublishBehavior(
        IWebhookEventPublisher publisher,
        IRaythaDbContext db,
        ILogger<WebhookPublishBehavior<TMessage, TResponse>> logger
    )
    {
        _publisher = publisher;
        _db = db;
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

        var transaction = _db.DbContext.Database.CurrentTransaction;
        if (transaction is null)
        {
            await TryPublishAsync(Attribute.EventName, message, response, cancellationToken);
            return response;
        }

        // Inside the command's transaction, swallowing the exception is not enough: a failed
        // statement aborts the Postgres transaction, and a delivery row saved before the failure
        // or an entity still tracked as Added would be carried into the audit row's SaveChanges.
        var tracker = _db.DbContext.ChangeTracker;
        var trackedBefore = tracker
            .Entries()
            .Select(e => e.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);
        await transaction.CreateSavepointAsync(Savepoint, cancellationToken);
        if (!await TryPublishAsync(Attribute.EventName, message, response, cancellationToken))
        {
            await transaction.RollbackToSavepointAsync(Savepoint, cancellationToken);
            foreach (var entry in tracker.Entries().Where(e => !trackedBefore.Contains(e.Entity)).ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        return response;
    }

    private async Task<bool> TryPublishAsync(
        string eventName,
        TMessage message,
        TResponse response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var payload = BuildPayload(message, response);
            await _publisher.PublishAsync(eventName, payload, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Webhook publish failed for {EventName} after {Request}",
                eventName,
                typeof(TMessage).Name
            );
            return false;
        }
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

    /// <summary>
    /// The payload delivered for <paramref name="message"/>. Public so a background task that runs
    /// the use case without the pipeline can publish the same event.
    /// </summary>
    public static object BuildPayload(TMessage message, TResponse response)
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
