using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raytha.Infrastructure.BackgroundTasks;
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    static readonly object _lockObject = new object();
    private readonly IRaythaDbContext _db;
    private readonly IBackgroundTaskDb _backgroundTaskDb;
    public BackgroundTaskQueue(IRaythaDbContext db, IBackgroundTaskDb backgroundTaskDb)
    {
        _db = db;
        _backgroundTaskDb = backgroundTaskDb;
    }

    public async ValueTask<Guid> EnqueueAsync<T>(object args, CancellationToken cancellationToken)
    {
        Guid jobId = Guid.NewGuid();

        _db.BackgroundTasks.Add(new BackgroundTask
        {
            Id = jobId,
            Name = typeof(T).AssemblyQualifiedName,
            Args = JsonSerializer.Serialize(args, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            })
        });

        await _db.SaveChangesAsync(cancellationToken);

        return jobId;
    }

    public async ValueTask<BackgroundTask> DequeueAsync(
        CancellationToken cancellationToken)
    {
        lock(_lockObject)
        {
            try
            {
                var workItem = _backgroundTaskDb.DequeueBackgroundTask();
                return workItem;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}