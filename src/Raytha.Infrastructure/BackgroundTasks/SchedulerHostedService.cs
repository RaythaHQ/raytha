using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.BackgroundTasks;

/// <summary>
/// Every <see cref="Interval"/>: marks processing tasks whose heartbeat lease lapsed as
/// abandoned, and enqueues a <see cref="DeliverWebhookTask"/> for each pending webhook
/// delivery whose <c>NextRetryAt</c> has arrived. Each step is one conditional UPDATE over
/// row locks, so any number of hosts may run it at the same time.
/// </summary>
public sealed class SchedulerHostedService : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Four missed 30 s heartbeats. Long enough that a database blip does not orphan a live
    /// import, short enough that a crashed host's tasks stop reading as "processing" quickly.
    /// </summary>
    public static readonly TimeSpan TaskLease = TimeSpan.FromMinutes(2);

    public const string AbandonedMessage =
        "Abandoned: the worker running this task stopped (host restart or crash).";

    public const int DeliveryBatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerHostedService> _logger;

    public SchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulerHostedService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
                if (db is DbConnection connection)
                {
                    await connection.OpenAsync(stoppingToken);
                }
                else
                {
                    db.Open();
                }

                var abandoned = await MarkAbandonedTasksAsync(db, stoppingToken);
                if (abandoned > 0)
                {
                    _logger.LogWarning(
                        "Marked {Count} background task(s) as abandoned after {Lease} without a heartbeat",
                        abandoned,
                        TaskLease
                    );
                }

                var enqueued = await EnqueueDueWebhookDeliveriesAsync(db, stoppingToken);
                if (enqueued > 0)
                {
                    _logger.LogDebug("Enqueued {Count} due webhook deliver(ies)", enqueued);
                }
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduler tick failed; retrying at the next interval");
            }
        }
    }

    private static Task<int> MarkAbandonedTasksAsync(
        IDbConnection db,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        return db.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE "BackgroundTasks"
                SET "Status" = @error,
                    "ErrorMessage" = @message,
                    "PercentComplete" = 100,
                    "CompletionTime" = @now,
                    "LastModificationTime" = @now
                WHERE "Status" = @processing
                  AND COALESCE("LastModificationTime", "CreationTime") < @cutoff
                """,
                new
                {
                    error = BackgroundTaskStatus.Error.DeveloperName,
                    message = AbandonedMessage,
                    now,
                    processing = BackgroundTaskStatus.Processing.DeveloperName,
                    cutoff = now - TaskLease,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    private static async Task<int> EnqueueDueWebhookDeliveriesAsync(
        IDbConnection db,
        CancellationToken cancellationToken
    )
    {
        var now = DateTime.UtcNow;
        using var transaction = db.BeginTransaction();
        var dueIds = (
            await db.QueryAsync<Guid>(
                new CommandDefinition(
                    """
                    UPDATE "WebhookDeliveries"
                    SET "NextRetryAt" = @leaseUntil
                    WHERE "Id" IN (
                        SELECT "Id" FROM "WebhookDeliveries"
                        WHERE "Status" = @pending AND "NextRetryAt" <= @now
                        ORDER BY "NextRetryAt"
                        LIMIT @batch
                        FOR UPDATE SKIP LOCKED
                    )
                    RETURNING "Id"
                    """,
                    new
                    {
                        leaseUntil = now + DeliverWebhookTask.InFlightLease,
                        pending = WebhookDeliveryStatus.Pending.DeveloperName,
                        now,
                        batch = DeliveryBatchSize,
                    },
                    transaction,
                    cancellationToken: cancellationToken
                )
            )
        ).ToList();

        if (dueIds.Count > 0)
        {
            var tasks = dueIds
                .Select(id =>
                    BackgroundTaskQueue.NewTask<DeliverWebhookTask>(
                        new DeliverWebhookTask.Args { DeliveryId = id }
                    )
                )
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    args = t.Args,
                    status = t.Status.DeveloperName,
                    creationTime = t.CreationTime,
                });
            await db.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO "BackgroundTasks"
                        ("Id", "Name", "Args", "Status", "PercentComplete", "NumberOfRetries", "CreationTime", "TaskStep")
                    VALUES (@id, @name, @args, @status, 0, 0, @creationTime, 0)
                    """,
                    tasks,
                    transaction,
                    cancellationToken: cancellationToken
                )
            );
        }

        transaction.Commit();
        return dueIds.Count;
    }
}
