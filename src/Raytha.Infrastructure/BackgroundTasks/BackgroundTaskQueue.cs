using System.Text.Json;
using System.Text.Json.Serialization;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Domain.JsonConverters;

namespace Raytha.Infrastructure.BackgroundTasks;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private static readonly JsonSerializerOptions ArgsJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new ShortGuidConverter() },
    };

    private readonly IRaythaDbContext _db;

    public BackgroundTaskQueue(IRaythaDbContext db)
    {
        _db = db;
    }

    public static BackgroundTask NewTask<T>(object args)
    {
        return new BackgroundTask
        {
            Id = Guid.NewGuid(),
            Name = typeof(T).AssemblyQualifiedName!,
            Args = JsonSerializer.Serialize(args, ArgsJsonOptions),
        };
    }

    public async ValueTask<Guid> EnqueueAsync<T>(object args, CancellationToken cancellationToken)
    {
        var task = NewTask<T>(args);
        _db.BackgroundTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        return task.Id;
    }
}
