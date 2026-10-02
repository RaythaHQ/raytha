using System.Data;
using System.Data.Common;
using Dapper;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.BackgroundTasks;

public class BackgroundTaskDb : IBackgroundTaskDb
{
    private readonly IDbConnection _db;

    public BackgroundTaskDb(IDbConnection db)
    {
        _db = db;
    }

    public async Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken)
    {
        await OpenAsync(cancellationToken);
        using var transaction = _db.BeginTransaction();
        var task = await _db.QueryFirstOrDefaultAsync<BackgroundTask>(
            new CommandDefinition(
                """
                SELECT * FROM "BackgroundTasks"
                WHERE "Status" = @enqueued
                ORDER BY "CreationTime" ASC
                LIMIT 1
                FOR UPDATE SKIP LOCKED
                """,
                new { enqueued = BackgroundTaskStatus.Enqueued.DeveloperName },
                transaction,
                cancellationToken: cancellationToken
            )
        );
        if (task != null)
        {
            // The lease clock starts at claim time, not enqueue time, so a task that waited
            // in a backlog is not swept as abandoned before its first heartbeat.
            await _db.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE "BackgroundTasks"
                    SET "Status" = @processing, "LastModificationTime" = @now
                    WHERE "Id" = @id
                    """,
                    new
                    {
                        processing = BackgroundTaskStatus.Processing.DeveloperName,
                        now = DateTime.UtcNow,
                        id = task.Id,
                    },
                    transaction,
                    cancellationToken: cancellationToken
                )
            );
        }
        transaction.Commit();
        return task;
    }

    public async Task HeartbeatAsync(Guid taskId, CancellationToken cancellationToken)
    {
        await OpenAsync(cancellationToken);
        await _db.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE "BackgroundTasks"
                SET "LastModificationTime" = @now
                WHERE "Id" = @id AND "Status" = @processing
                """,
                new
                {
                    now = DateTime.UtcNow,
                    id = taskId,
                    processing = BackgroundTaskStatus.Processing.DeveloperName,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task CompleteAsync(
        Guid taskId,
        BackgroundTaskStatus status,
        string? errorMessage,
        CancellationToken cancellationToken
    )
    {
        await OpenAsync(cancellationToken);
        await _db.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE "BackgroundTasks"
                SET "Status" = @status,
                    "ErrorMessage" = @errorMessage,
                    "PercentComplete" = 100,
                    "CompletionTime" = @now,
                    "LastModificationTime" = @now
                WHERE "Id" = @id
                """,
                new
                {
                    status = status.DeveloperName,
                    errorMessage,
                    now = DateTime.UtcNow,
                    id = taskId,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    private async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_db.State != ConnectionState.Closed)
        {
            return;
        }
        if (_db is DbConnection connection)
        {
            await connection.OpenAsync(cancellationToken);
        }
        else
        {
            _db.Open();
        }
    }
}
