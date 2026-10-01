using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.BackgroundTasks;

namespace Raytha.Infrastructure.UnitTests.BackgroundTasks;

[TestFixture]
public class BackgroundTaskQueueTests
{
    private Mock<IRaythaDbContext> _mockDbContext = null!;
    private List<BackgroundTask> _added = null!;
    private BackgroundTaskQueue _queue = null!;

    [SetUp]
    public void Setup()
    {
        _added = new List<BackgroundTask>();
        var set = new Mock<DbSet<BackgroundTask>>();
        set.Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task =>
            {
                lock (_added)
                {
                    _added.Add(task);
                }
            });

        _mockDbContext = new Mock<IRaythaDbContext>();
        _mockDbContext.Setup(x => x.BackgroundTasks).Returns(set.Object);
        _mockDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _queue = new BackgroundTaskQueue(_mockDbContext.Object);
    }

    [Test]
    public async Task Enqueue_adds_one_row_named_after_the_task_type_and_returns_its_id()
    {
        var jobId = await _queue.EnqueueAsync<TestBackgroundTask>(new { }, CancellationToken.None);

        _added.Should().ContainSingle();
        _added[0].Id.Should().Be(jobId);
        _added[0].Name.Should().Be(typeof(TestBackgroundTask).AssemblyQualifiedName);
        _added[0].Status.Should().Be(BackgroundTaskStatus.Enqueued);
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Enqueue_serializes_args_as_json_the_worker_can_read_back()
    {
        await _queue.EnqueueAsync<TestBackgroundTask>(
            new { Name = "TestJob", Priority = 5, IsUrgent = true, Items = new[] { "a", "b" } },
            CancellationToken.None
        );

        var args = JsonSerializer.Deserialize<JsonElement>(_added[0].Args!);
        args.GetProperty("Name").GetString().Should().Be("TestJob");
        args.GetProperty("Priority").GetInt32().Should().Be(5);
        args.GetProperty("IsUrgent").GetBoolean().Should().BeTrue();
        args.GetProperty("Items").GetArrayLength().Should().Be(2);
    }

    [Test]
    public async Task Enqueue_with_null_args_stores_json_null()
    {
        await _queue.EnqueueAsync<TestBackgroundTask>(null!, CancellationToken.None);

        _added[0].Args.Should().Be("null");
    }

    [Test]
    public async Task Concurrent_enqueues_each_get_a_distinct_id()
    {
        var ids = await Task.WhenAll(
            Enumerable
                .Range(0, 20)
                .Select(i =>
                    _queue
                        .EnqueueAsync<TestBackgroundTask>(new { Index = i }, CancellationToken.None)
                        .AsTask()
                )
        );

        ids.Should().OnlyHaveUniqueItems();
        _added.Select(t => t.Id).Should().BeEquivalentTo(ids);
    }

    private class TestBackgroundTask { }
}
