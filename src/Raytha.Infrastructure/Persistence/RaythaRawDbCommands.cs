using System.Data;
using Dapper;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Persistence;

/// <summary>
/// Executes raw database commands that require database-specific SQL.
/// </summary>
public class RaythaRawDbCommands : IRaythaRawDbCommands
{
    private readonly IDbConnection _db;

    public RaythaRawDbCommands(IDbConnection db)
    {
        _db = db;
    }

    public async Task ClearAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync("TRUNCATE TABLE \"AuditLogs\"");
    }

    public async Task ClearEmailLogsAsync(CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync("TRUNCATE TABLE \"EmailLogs\"");
    }

    public async Task ClearWebhookDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        await _db.ExecuteAsync("TRUNCATE TABLE \"WebhookDeliveries\"");
    }

    public async Task<int> ClearCompletedBackgroundTasksAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _db.ExecuteAsync(
            "DELETE FROM \"BackgroundTasks\" WHERE \"Status\" IN (@complete, @error)",
            new
            {
                complete = BackgroundTaskStatus.Complete.DeveloperName,
                error = BackgroundTaskStatus.Error.DeveloperName,
            }
        );
    }
}
