using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.BackgroundTasks;

public class QueuedHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public QueuedHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IBackgroundTaskQueue _taskQueue =
                scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();

            IRaythaDbContext _db = scope.ServiceProvider.GetRequiredService<IRaythaDbContext>();

            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(1000);
                var dqTask = await _taskQueue.DequeueAsync(stoppingToken);
                if (dqTask == null)
                    continue;

                var backgroundTask = _db.BackgroundTasks.First(p => p.Id == dqTask.Id);

                try
                {
                    IExecuteBackgroundTask scopedProcessingService =
                        scope.ServiceProvider.GetRequiredService(Type.GetType(backgroundTask.Name))
                        as IExecuteBackgroundTask;

                    await scopedProcessingService.Execute(
                        backgroundTask.Id,
                        JsonSerializer.Deserialize<JsonElement>(backgroundTask.Args),
                        stoppingToken
                    );
                    backgroundTask.Status = BackgroundTaskStatus.Complete;
                }
                catch (Exception ex)
                {
                    backgroundTask.Status = BackgroundTaskStatus.Error;
                    backgroundTask.ErrorMessage = ex.Message;
                }
                backgroundTask.PercentComplete = 100;
                backgroundTask.CompletionTime = DateTime.UtcNow;
                _db.BackgroundTasks.Update(backgroundTask);
                await _db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}
