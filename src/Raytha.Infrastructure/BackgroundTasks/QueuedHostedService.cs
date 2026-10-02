using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.BackgroundTasks;

/// <summary>
/// One worker over the BackgroundTasks queue. Each iteration claims a task in a fresh DI scope,
/// runs it while heartbeating its row, and records the outcome through
/// <see cref="IBackgroundTaskDb"/>. No failure inside an iteration escapes
/// <see cref="ExecuteAsync"/>; only host shutdown ends the loop.
/// </summary>
public class QueuedHostedService : BackgroundService
{
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);
    public const string HostStoppedMessage = "The host stopped before this task finished.";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _heartbeatInterval;

    public QueuedHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedHostedService> logger
    )
        : this(scopeFactory, logger, DefaultPollInterval, DefaultHeartbeatInterval) { }

    internal QueuedHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedHostedService> logger,
        TimeSpan pollInterval,
        TimeSpan heartbeatInterval
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollInterval = pollInterval;
        _heartbeatInterval = heartbeatInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            var ranOne = false;
            try
            {
                ranOne = await RunOneAsync(stoppingToken);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background worker iteration failed; the worker keeps polling");
            }

            if (!ranOne)
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> RunOneAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var taskDb = scope.ServiceProvider.GetRequiredService<IBackgroundTaskDb>();
        var task = await taskDb.DequeueAsync(stoppingToken);
        if (task is null)
        {
            return false;
        }

        var (status, errorMessage) = await RunTaskAsync(scope.ServiceProvider, task, stoppingToken);
        await taskDb.CompleteAsync(task.Id, status, errorMessage, CancellationToken.None);
        return true;
    }

    private async Task<(BackgroundTaskStatus Status, string? ErrorMessage)> RunTaskAsync(
        IServiceProvider services,
        BackgroundTask task,
        CancellationToken stoppingToken
    )
    {
        using var heartbeatStop = new CancellationTokenSource();
        var heartbeat = HeartbeatAsync(
            services.GetRequiredService<IBackgroundTaskDb>(),
            task.Id,
            heartbeatStop.Token
        );
        try
        {
            var type =
                Type.GetType(task.Name)
                ?? throw new InvalidOperationException(
                    $"Background task type '{task.Name}' is not loaded."
                );
            var job = (IExecuteBackgroundTask)services.GetRequiredService(type);
            await job.Execute(
                task.Id,
                JsonSerializer.Deserialize<JsonElement>(task.Args ?? "null"),
                stoppingToken
            );
            return (BackgroundTaskStatus.Complete, null);
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            return (BackgroundTaskStatus.Error, HostStoppedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background task {TaskId} ({TaskName}) failed", task.Id, task.Name);
            return (BackgroundTaskStatus.Error, ex.Message);
        }
        finally
        {
            heartbeatStop.Cancel();
            await heartbeat;
        }
    }

    private async Task HeartbeatAsync(
        IBackgroundTaskDb taskDb,
        Guid taskId,
        CancellationToken stop
    )
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_heartbeatInterval, stop);
                await taskDb.HeartbeatAsync(taskId, stop);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Heartbeat for background task {TaskId} failed; it is marked abandoned if this persists",
                    taskId
                );
            }
        }
    }
}
