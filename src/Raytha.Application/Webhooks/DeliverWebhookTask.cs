using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks;

/// <summary>
/// Makes one HTTP attempt for one <see cref="WebhookDelivery"/> and returns the worker.
/// A failed attempt with attempts remaining leaves the delivery pending and sets
/// <see cref="WebhookDelivery.NextRetryAt"/> to now plus a capped exponential backoff; the
/// scheduler enqueues a new run when that time arrives. The final failure (per
/// <see cref="Webhook.MaxAttempts"/>) marks the delivery failed for manual redelivery.
/// <para>
/// <see cref="WebhookDelivery.NextRetryAt"/> doubles as the in-flight lease: whoever hands a
/// delivery to a job sets it to now plus <see cref="InFlightLease"/>, so a delivery whose
/// job died is retried once the lease lapses. Delivery is at-least-once; receivers dedupe
/// on the X-Raytha-Delivery header. A run that finds the delivery already settled exits
/// without a request.
/// </para>
/// </summary>
public class DeliverWebhookTask : IExecuteBackgroundTask
{
    public const string HttpClientName = "raytha-webhooks";

    /// <summary>
    /// How long a pending delivery is considered owned by an enqueued job before the
    /// scheduler may hand it out again. Covers the longest allowed per-attempt timeout
    /// (120 s) plus generous queue wait.
    /// </summary>
    public static readonly TimeSpan InFlightLease = TimeSpan.FromMinutes(10);

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
            await FinishAsync(job, $"Webhook delivery {deliveryId} no longer exists.", cancellationToken);
            return;
        }

        if (!delivery.Status.Equals(WebhookDeliveryStatus.Pending))
        {
            await FinishAsync(job, $"Delivery already {delivery.Status.DeveloperName}.", cancellationToken);
            return;
        }

        var webhook = await _db
            .Webhooks.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == delivery.WebhookId, cancellationToken);

        if (webhook is null)
        {
            delivery.ErrorMessage = "The webhook was deleted.";
            Settle(delivery, WebhookDeliveryStatus.Failed);
            await FinishAsync(job, delivery.ErrorMessage, cancellationToken);
            return;
        }

        var maxAttempts = Math.Max(1, webhook.MaxAttempts);
        job.TaskStep = delivery.AttemptCount + 1;
        job.StatusInfo = $"Attempt {delivery.AttemptCount + 1} of {maxAttempts}: {webhook.Url}";
        await _db.SaveChangesAsync(cancellationToken);

        if (await AttemptAsync(webhook, delivery, cancellationToken))
        {
            Settle(delivery, WebhookDeliveryStatus.Succeeded);
            await FinishAsync(
                job,
                $"Delivered to {webhook.Url} (HTTP {delivery.ResponseCode}).",
                cancellationToken
            );
            return;
        }

        if (delivery.AttemptCount >= maxAttempts)
        {
            Settle(delivery, WebhookDeliveryStatus.Failed);
            await FinishAsync(
                job,
                $"Failed after {delivery.AttemptCount} attempt(s): {delivery.ErrorMessage}",
                cancellationToken
            );
            _logger.LogWarning(
                "Webhook delivery {DeliveryId} to {WebhookName} failed permanently: {Error}",
                delivery.Id,
                webhook.Name,
                delivery.ErrorMessage
            );
            return;
        }

        var backoff = ComputeBackoff(delivery.AttemptCount);
        delivery.NextRetryAt = DateTime.UtcNow.Add(backoff);
        await FinishAsync(
            job,
            $"Attempt {delivery.AttemptCount} of {maxAttempts} failed ({delivery.ErrorMessage}); "
                + $"retry in {backoff.TotalSeconds:0}s at {delivery.NextRetryAt:yyyy-MM-dd HH:mm:ss} UTC.",
            cancellationToken
        );
    }

    private static void Settle(WebhookDelivery delivery, WebhookDeliveryStatus status)
    {
        delivery.Status = status;
        delivery.NextRetryAt = null;
        delivery.CompletionTime = DateTime.UtcNow;
    }

    private async Task FinishAsync(BackgroundTask job, string statusInfo, CancellationToken cancellationToken)
    {
        job.StatusInfo = statusInfo;
        job.PercentComplete = 100;
        await _db.SaveChangesAsync(cancellationToken);
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
        // Host shutdown is not an attempt: let it propagate unsaved so the lease hands the
        // delivery back to the scheduler.
        catch (Exception ex)
            when (ex is HttpRequestException or TaskCanceledException
                && !cancellationToken.IsCancellationRequested
            )
        {
            stopwatch.Stop();
            delivery.DurationMs = stopwatch.ElapsedMilliseconds;
            delivery.ResponseCode = null;
            delivery.ResponseBody = null;
            delivery.ErrorMessage =
                ex is TaskCanceledException ? $"Timed out after {webhook.TimeoutSeconds}s." : ex.Message;
            return false;
        }
    }
}
