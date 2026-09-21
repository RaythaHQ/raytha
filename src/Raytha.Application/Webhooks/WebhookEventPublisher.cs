using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Domain.JsonConverters;

namespace Raytha.Application.Webhooks;

public sealed class WebhookEventPublisher : IWebhookEventPublisher
{
    public static readonly JsonSerializerOptions PayloadJsonOptions = new(
        JsonSerializerDefaults.Web
    )
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new ShortGuidConverter() },
    };

    private readonly IRaythaDbContext _db;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ILogger<WebhookEventPublisher> _logger;

    public WebhookEventPublisher(
        IRaythaDbContext db,
        IBackgroundTaskQueue taskQueue,
        ILogger<WebhookEventPublisher> logger
    )
    {
        _db = db;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public async Task PublishAsync(
        string eventName,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var candidates = await _db
            .Webhooks.AsNoTracking()
            .Where(w => w.IsActive)
            .ToListAsync(cancellationToken);

        var targets = candidates.Where(w => w.SubscribesTo(eventName)).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var webhook in targets)
        {
            try
            {
                await CreateAndEnqueueAsync(webhook, eventName, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to enqueue webhook delivery for {EventName} to {WebhookName}",
                    eventName,
                    webhook.Name
                );
            }
        }
    }

    public async Task<Guid> PublishToAsync(
        Guid webhookId,
        string eventName,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var webhook = await _db
            .Webhooks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == webhookId, cancellationToken);
        if (webhook is null)
        {
            throw new NotFoundException("Webhook", webhookId);
        }

        return await CreateAndEnqueueAsync(webhook, eventName, payload, cancellationToken);
    }

    private async Task<Guid> CreateAndEnqueueAsync(
        Webhook webhook,
        string eventName,
        object? payload,
        CancellationToken cancellationToken
    )
    {
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            WebhookId = webhook.Id,
            EventName = eventName,
            Status = WebhookDeliveryStatus.Pending,
        };

        delivery.Payload = JsonSerializer.Serialize(
            new
            {
                id = delivery.Id,
                @event = eventName,
                timestamp = delivery.CreationTime,
                data = payload,
            },
            PayloadJsonOptions
        );

        _db.WebhookDeliveries.Add(delivery);
        await _db.SaveChangesAsync(cancellationToken);

        await _taskQueue.EnqueueAsync<DeliverWebhookTask>(
            new DeliverWebhookTask.Args { DeliveryId = delivery.Id },
            cancellationToken
        );

        return delivery.Id;
    }
}
