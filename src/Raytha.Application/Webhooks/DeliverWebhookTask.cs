using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks;

/// <summary>
/// Delivers one <see cref="WebhookDelivery"/> over HTTP. Runs on the existing
/// background task queue. Failed attempts are retried in-process with exponential
/// backoff (capped) until <see cref="Webhook.MaxAttempts"/> is exhausted, at which
/// point the delivery is marked failed and can be redelivered manually.
/// </summary>
public class DeliverWebhookTask : IExecuteBackgroundTask
{
    public const string HttpClientName = "raytha-webhooks";

    public record Args
    {
        public Guid DeliveryId { get; init; }
    }

    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    private readonly IRaythaDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DeliverWebhookTask> _logger;

    public DeliverWebhookTask(
        IRaythaDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<DeliverWebhookTask> logger
    )
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public static TimeSpan ComputeBackoff(int attemptNumber)
    {
        if (attemptNumber < 1)
        {
            attemptNumber = 1;
        }

        var exponent = Math.Min(attemptNumber - 1, 10);
        var delay = TimeSpan.FromTicks(BaseBackoff.Ticks * (1L << exponent));
        return delay > MaxBackoff ? MaxBackoff : delay;
    }

    public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
    {
        var deliveryId = args.GetProperty(nameof(Args.DeliveryId)).GetGuid();

        var job = await _db.BackgroundTasks.FirstAsync(p => p.Id == jobId, cancellationToken);
        var delivery = await _db.WebhookDeliveries.FirstOrDefaultAsync(
            d => d.Id == deliveryId,
            cancellationToken
        );

        if (delivery is null)
        {
            job.StatusInfo = $"Webhook delivery {deliveryId} no longer exists.";
            job.PercentComplete = 100;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (delivery.Status.Equals(WebhookDeliveryStatus.Succeeded))
        {
            job.StatusInfo = "Delivery already succeeded.";
            job.PercentComplete = 100;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var webhook = await _db
            .Webhooks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == delivery.WebhookId, cancellationToken);

        if (webhook is null)
        {
            delivery.Status = WebhookDeliveryStatus.Failed;
            delivery.ErrorMessage = "The webhook was deleted.";
            delivery.CompletionTime = DateTime.UtcNow;
            job.StatusInfo = delivery.ErrorMessage;
            job.PercentComplete = 100;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var maxAttempts = Math.Max(1, webhook.MaxAttempts);
        var attemptsThisRun = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            attemptsThisRun++;
            job.TaskStep = delivery.AttemptCount + 1;
            job.StatusInfo = $"Attempt {delivery.AttemptCount + 1} of {maxAttempts}: {webhook.Url}";
            job.PercentComplete = (int)(100.0 * delivery.AttemptCount / maxAttempts);
            await _db.SaveChangesAsync(cancellationToken);

            var succeeded = await AttemptAsync(webhook, delivery, cancellationToken);

            if (succeeded)
            {
                delivery.Status = WebhookDeliveryStatus.Succeeded;
                delivery.NextRetryAt = null;
                delivery.CompletionTime = DateTime.UtcNow;
                job.StatusInfo = $"Delivered to {webhook.Url} (HTTP {delivery.ResponseCode}).";
                job.PercentComplete = 100;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            if (delivery.AttemptCount >= maxAttempts)
            {
                delivery.Status = WebhookDeliveryStatus.Failed;
                delivery.NextRetryAt = null;
                delivery.CompletionTime = DateTime.UtcNow;
                job.StatusInfo =
                    $"Failed after {delivery.AttemptCount} attempt(s): {delivery.ErrorMessage}";
                job.PercentComplete = 100;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(
                    "Webhook delivery {DeliveryId} to {WebhookName} failed permanently: {Error}",
                    delivery.Id,
                    webhook.Name,
                    delivery.ErrorMessage
                );
                return;
            }

            var backoff = ComputeBackoff(attemptsThisRun);
            delivery.NextRetryAt = DateTime.UtcNow.Add(backoff);
            job.StatusInfo =
                $"Attempt {delivery.AttemptCount} failed ({delivery.ErrorMessage}); retrying in {backoff.TotalSeconds:0}s.";
            await _db.SaveChangesAsync(cancellationToken);

            await Task.Delay(backoff, cancellationToken);
        }
    }

    private async Task<bool> AttemptAsync(
        Webhook webhook,
        WebhookDelivery delivery,
        CancellationToken cancellationToken
    )
    {
        delivery.AttemptCount++;
        delivery.LastAttemptAt = DateTime.UtcNow;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, webhook.TimeoutSeconds));

            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
            request.Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            request.Headers.Add(WebhookSigner.EventHeader, delivery.EventName);
            request.Headers.Add(WebhookSigner.DeliveryHeader, delivery.Id.ToString());
            request.Headers.Add(
                WebhookSigner.TimestampHeader,
                delivery.LastAttemptAt.Value.ToString("O")
            );
            request.Headers.Add(
                WebhookSigner.SignatureHeader,
                WebhookSigner.Sign(delivery.Payload, webhook.Secret)
            );

            using var response = await client.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            delivery.ResponseCode = (int)response.StatusCode;
            delivery.ResponseBody =
                body.Length > WebhookDelivery.MaxResponseBodyLength
                    ? body[..WebhookDelivery.MaxResponseBodyLength]
                    : body;
            delivery.DurationMs = stopwatch.ElapsedMilliseconds;

            if (response.IsSuccessStatusCode)
            {
                delivery.ErrorMessage = null;
                return true;
            }

            delivery.ErrorMessage = $"Received HTTP {(int)response.StatusCode}.";
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            delivery.DurationMs = stopwatch.ElapsedMilliseconds;
            delivery.ResponseCode = null;
            delivery.ResponseBody = null;
            delivery.ErrorMessage =
                ex is TaskCanceledException && !cancellationToken.IsCancellationRequested
                    ? $"Timed out after {webhook.TimeoutSeconds}s."
                    : ex.Message;
            return false;
        }
    }
}
