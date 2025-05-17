using Raytha.Domain.Entities;

namespace Raytha.Application.Common.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask<Guid> EnqueueAsync<T>(object args, CancellationToken cancellationToken);

    ValueTask<BackgroundTask> DequeueAsync(CancellationToken cancellationToken);
}
