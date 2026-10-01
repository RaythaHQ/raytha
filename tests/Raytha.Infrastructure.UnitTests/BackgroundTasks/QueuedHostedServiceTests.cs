using System.Collections.Concurrent;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.BackgroundTasks;

namespace Raytha.Infrastructure.UnitTests.BackgroundTasks;

[TestFixture]
public class QueuedHostedServiceTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    private FakeTaskDb _taskDb = null!;
    private RecordingTask.Log _log = null!;
    private ServiceProvider _services = null!;
    private QueuedHostedService _worker = null!;

    [SetUp]
    public void Setup()
    {
        _taskDb = new FakeTaskDb();
        _log = new RecordingTask.Log();
        _services = new ServiceCollection()
            .AddSingleton<IBackgroundTaskDb>(_taskDb)
            .AddSingleton(_log)
            .AddTransient<RecordingTask>()
            .AddTransient<ThrowingTask>()
            .AddTransient<WaitingTask>()
            .BuildServiceProvider();
        _worker = new QueuedHostedService(
            _services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<QueuedHostedService>.Instance,
            pollInterval: TimeSpan.FromMilliseconds(10),
            heartbeatInterval: TimeSpan.FromMilliseconds(20)
        );
    }

    [TearDown]
    public async Task TearDown()
    {
        await _worker.StopAsync(CancellationToken.None);
        _worker.Dispose();
        await _services.DisposeAsync();
    }

    [Test]
    public async Task A_job_that_finishes_is_recorded_complete_with_its_args_delivered()
    {
        var task = Enqueue<RecordingTask>(new { Value = 42 });

        await _worker.StartAsync(CancellationToken.None);
        await _taskDb.WaitForCompletion(task.Id);

        _taskDb.Completed[task.Id].Should().Be((BackgroundTaskStatus.Complete, (string?)null));
        _log.Runs.Should().Equal((task.Id, 42));
    }

    [Test]
    public async Task A_job_that_throws_is_recorded_as_error_and_the_next_job_still_runs()
    {
        var failing = Enqueue<ThrowingTask>(new { });
        var next = Enqueue<RecordingTask>(new { Value = 1 });

        await _worker.StartAsync(CancellationToken.None);
        await _taskDb.WaitForCompletion(next.Id);

        _taskDb.Completed[failing.Id].Should().Be((BackgroundTaskStatus.Error, "boom"));
        _taskDb.Completed[next.Id].Should().Be((BackgroundTaskStatus.Complete, (string?)null));
    }

    [Test]
    public async Task A_task_whose_type_is_not_loaded_is_recorded_as_error()
    {
        var task = _taskDb.Add(
            new BackgroundTask { Id = Guid.NewGuid(), Name = "Nope.Missing, Nope", Args = "{}" }
        );

        await _worker.StartAsync(CancellationToken.None);
        await _taskDb.WaitForCompletion(task.Id);

        _taskDb
            .Completed[task.Id]
            .Should()
            .Be(
                (BackgroundTaskStatus.Error, "Background task type 'Nope.Missing, Nope' is not loaded.")
            );
    }

    [Test]
    public async Task A_dequeue_failure_does_not_stop_the_worker()
    {
        _taskDb.FailNextDequeueWith(new InvalidOperationException("database is away"));
        var task = Enqueue<RecordingTask>(new { Value = 7 });

        await _worker.StartAsync(CancellationToken.None);
        await _taskDb.WaitForCompletion(task.Id);

        _taskDb.Completed[task.Id].Should().Be((BackgroundTaskStatus.Complete, (string?)null));
        _taskDb.DequeueCalls.Should().BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task A_failed_final_status_write_does_not_stop_the_worker()
    {
        var first = Enqueue<RecordingTask>(new { Value = 1 });
        var second = Enqueue<RecordingTask>(new { Value = 2 });
        _taskDb.FailCompleteFor(first.Id, new InvalidOperationException("row vanished"));

        await _worker.StartAsync(CancellationToken.None);
        await _taskDb.WaitForCompletion(second.Id);

        _log.Runs.Should().Equal((first.Id, 1), (second.Id, 2));
        _taskDb.Completed.Should().NotContainKey(first.Id);
        _taskDb.Completed[second.Id].Should().Be((BackgroundTaskStatus.Complete, (string?)null));
    }

    [Test]
    public async Task A_job_interrupted_by_shutdown_ends_as_error_saying_the_host_stopped()
    {
        var task = Enqueue<WaitingTask>(new { });

        await _worker.StartAsync(CancellationToken.None);
        await _log.Started.Task.WaitAsync(WaitLimit);
        await _worker.StopAsync(CancellationToken.None);

        _taskDb.Completed[task.Id].Should().Be((BackgroundTaskStatus.Error, QueuedHostedService.HostStoppedMessage));
    }

    [Test]
    public async Task A_running_job_heartbeats_its_row_until_it_finishes()
    {
        var task = Enqueue<WaitingTask>(new { });

        await _worker.StartAsync(CancellationToken.None);
        await _log.Started.Task.WaitAsync(WaitLimit);
        await _taskDb.WaitForHeartbeats(task.Id, 3);
        _log.Release.SetResult();
        await _taskDb.WaitForCompletion(task.Id);

        _taskDb.Completed[task.Id].Should().Be((BackgroundTaskStatus.Complete, (string?)null));
        var beats = _taskDb.Heartbeats[task.Id];
        await Task.Delay(100);
        _taskDb.Heartbeats[task.Id].Should().Be(beats, "the heartbeat stops with the job");
    }

    private BackgroundTask Enqueue<T>(object args)
    {
        return _taskDb.Add(
            new BackgroundTask
            {
                Id = Guid.NewGuid(),
                Name = typeof(T).AssemblyQualifiedName!,
                Args = JsonSerializer.Serialize(args),
            }
        );
    }

    private sealed class FakeTaskDb : IBackgroundTaskDb
    {
        private readonly ConcurrentQueue<BackgroundTask> _pending = new();
        private readonly ConcurrentDictionary<Guid, Exception> _completeFaults = new();
        private Exception? _dequeueFault;
        private int _dequeueCalls;

        public ConcurrentDictionary<Guid, (BackgroundTaskStatus, string?)> Completed { get; } = new();
        public ConcurrentDictionary<Guid, int> Heartbeats { get; } = new();
        public int DequeueCalls => _dequeueCalls;

        public BackgroundTask Add(BackgroundTask task)
        {
            _pending.Enqueue(task);
            return task;
        }

        public void FailNextDequeueWith(Exception ex) => _dequeueFault = ex;

        public void FailCompleteFor(Guid taskId, Exception ex) => _completeFaults[taskId] = ex;

        public Task<BackgroundTask?> DequeueAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _dequeueCalls);
            var fault = Interlocked.Exchange(ref _dequeueFault, null);
            if (fault != null)
            {
                throw fault;
            }
            return Task.FromResult(_pending.TryDequeue(out var task) ? task : null);
        }

        public Task HeartbeatAsync(Guid taskId, CancellationToken cancellationToken)
        {
            Heartbeats.AddOrUpdate(taskId, 1, (_, n) => n + 1);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(
            Guid taskId,
            BackgroundTaskStatus status,
            string? errorMessage,
            CancellationToken cancellationToken
        )
        {
            if (_completeFaults.TryRemove(taskId, out var fault))
            {
                throw fault;
            }
            Completed[taskId] = (status, errorMessage);
            return Task.CompletedTask;
        }

        public Task WaitForCompletion(Guid taskId) => WaitUntil(() => Completed.ContainsKey(taskId));

        public Task WaitForHeartbeats(Guid taskId, int count) =>
            WaitUntil(() => Heartbeats.TryGetValue(taskId, out var n) && n >= count);

        private static async Task WaitUntil(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow + WaitLimit;
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("The worker did not reach the expected state.");
                }
                await Task.Delay(5);
            }
        }
    }

    private sealed class RecordingTask : IExecuteBackgroundTask
    {
        public sealed class Log
        {
            public List<(Guid JobId, int Value)> Runs { get; } = new();
            public TaskCompletionSource Started { get; } = new();
            public TaskCompletionSource Release { get; } = new();
        }

        private readonly Log _log;

        public RecordingTask(Log log) => _log = log;

        public Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            lock (_log.Runs)
            {
                _log.Runs.Add((jobId, args.GetProperty("Value").GetInt32()));
            }
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingTask : IExecuteBackgroundTask
    {
        public Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class WaitingTask : IExecuteBackgroundTask
    {
        private readonly RecordingTask.Log _log;

        public WaitingTask(RecordingTask.Log log) => _log = log;

        public async Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken)
        {
            _log.Started.TrySetResult();
            await _log.Release.Task.WaitAsync(cancellationToken);
        }
    }
}
