using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raytha.Application.Maintenance.Commands;

namespace Raytha.Infrastructure.Maintenance;

/// <summary>
/// Applies log retention shortly after startup and then daily at 03:00 UTC. Every run is a
/// set-based delete, so overlapping runs across replicas converge on the same result.
/// </summary>
public sealed class LogRetentionHostedService : BackgroundService
{
    public static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogRetentionHostedService> _logger;

    public LogRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LogRetentionHostedService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = StartupDelay;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, stoppingToken);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
                await mediator.Send(new PurgeExpiredLogs.Command(), stoppingToken);
            }
            catch (Exception ex)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                _logger.LogError(ex, "Log retention purge failed; retrying at the next scheduled run");
            }

            delay = DelayUntilNextRun(DateTimeOffset.UtcNow);
        }
    }

    public static TimeSpan DelayUntilNextRun(DateTimeOffset utcNow)
    {
        var next = new DateTimeOffset(utcNow.UtcDateTime.Date.AddHours(3), TimeSpan.Zero);
        if (utcNow >= next)
        {
            next = next.AddDays(1);
        }
        return next - utcNow;
    }
}
