using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Row-level access to the BackgroundTasks table for the worker loop, on its own connection.
/// A job's own DbContext is often the thing that just failed, so the worker never writes a
/// task's lifecycle through it.
/// </summary>
public interface IBackgroundTaskDb
{
    /// <summary>Claims the oldest enqueued task for this worker, or null when the queue is empty.</summary>
    Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>Renews the lease on a running task. A no-op once the task is no longer processing.</summary>
    Task HeartbeatAsync(Guid taskId, CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid taskId,
        BackgroundTaskStatus status,
        string? errorMessage,
        CancellationToken cancellationToken
    );
}
