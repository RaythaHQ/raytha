namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Interface for executing raw database commands that require database-specific SQL.
/// </summary>
public interface IRaythaRawDbCommands
{
    /// <summary>
    /// Clears all audit logs from the database using TRUNCATE for optimal performance.
    /// </summary>
    Task ClearAuditLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all email log entries using TRUNCATE.
    /// </summary>
    Task ClearEmailLogsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all webhook delivery records using TRUNCATE. Webhook subscriptions are kept.
    /// </summary>
    Task ClearWebhookDeliveriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes background tasks that have finished (complete or error). Returns rows removed.
    /// </summary>
    Task<int> ClearCompletedBackgroundTasksAsync(CancellationToken cancellationToken = default);
}
