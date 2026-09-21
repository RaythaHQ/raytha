using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Commands;

/// <summary>
/// Enqueues a harmless background task that ticks through a fixed number of steps, so
/// operators can verify the worker queue, progress reporting, and polling end to end.
/// </summary>
public class BeginDemoProgressTask
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public int Steps { get; init; } = 10;
        public int DelayMs { get; init; } = 500;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Steps).InclusiveBetween(1, 100);
            RuleFor(x => x.DelayMs).InclusiveBetween(0, 5000);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IBackgroundTaskQueue _taskQueue;

        public Handler(IBackgroundTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var jobId = await _taskQueue.EnqueueAsync<DemoProgressTask>(
                new DemoProgressTask.Args { Steps = request.Steps, DelayMs = request.DelayMs },
                cancellationToken
            );

            return new CommandResponseDto<ShortGuid>(jobId);
        }
    }
}

public class DemoProgressTask : IExecuteBackgroundTask
{
    public record Args
    {
        public int Steps { get; init; } = 10;
        public int DelayMs { get; init; } = 500;
    }

    private readonly IRaythaDbContext _db;

    public DemoProgressTask(IRaythaDbContext db)
    {
        _db = db;
    }

    public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
    {
        var steps = Math.Clamp(
            args.TryGetProperty(nameof(Args.Steps), out var s) ? s.GetInt32() : 10,
            1,
            100
        );
        var delayMs = Math.Clamp(
            args.TryGetProperty(nameof(Args.DelayMs), out var d) ? d.GetInt32() : 500,
            0,
            5000
        );

        var job = _db.BackgroundTasks.First(p => p.Id == jobId);

        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            job.TaskStep = step;
            job.StatusInfo = $"Demo step {step} of {steps}";
            job.PercentComplete = (int)Math.Round(100.0 * step / steps);
            _db.BackgroundTasks.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            if (delayMs > 0 && step < steps)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        job.StatusInfo = $"Demo task finished ({steps} steps).";
        job.PercentComplete = 100;
        _db.BackgroundTasks.Update(job);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
