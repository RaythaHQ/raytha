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
    private Mock<IRaythaDbContext> _mockDbContext;
    private Mock<IBackgroundTaskDb> _mockBackgroundTaskDb;
    private Mock<DbSet<BackgroundTask>> _mockBackgroundTaskSet;

    [SetUp]
    public void Setup()
    {
        _mockDbContext = new Mock<IRaythaDbContext>();
        _mockBackgroundTaskDb = new Mock<IBackgroundTaskDb>();
        _mockBackgroundTaskSet = new Mock<DbSet<BackgroundTask>>();

        _mockDbContext.Setup(x => x.BackgroundTasks).Returns(_mockBackgroundTaskSet.Object);
    }

    #region EnqueueAsync Tests

    [Test]
    public async Task EnqueueAsync_ShouldReturnNewGuid()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        var jobId = await queue.EnqueueAsync<TestBackgroundTask>(
            new { TestProperty = "test" },
            CancellationToken.None
        );

        // Assert
        jobId.Should().NotBe(Guid.Empty);
    }

    [Test]
    public async Task EnqueueAsync_ShouldAddTaskToDbContext()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(
            new { Name = "Test", Value = 42 },
            CancellationToken.None
        );

        // Assert
        _mockBackgroundTaskSet.Verify(x => x.Add(It.IsAny<BackgroundTask>()), Times.Once);
        addedTask.Should().NotBeNull();
    }

    [Test]
    public async Task EnqueueAsync_ShouldSetTaskNameToAssemblyQualifiedName()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(new { }, CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.Name.Should().Contain(nameof(TestBackgroundTask));
        addedTask.Name.Should().Contain(typeof(TestBackgroundTask).Assembly.FullName?.Split(',')[0]);
    }

    [Test]
    public async Task EnqueueAsync_ShouldSerializeArgsToJson()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var args = new { Name = "TestJob", Priority = 5, IsUrgent = true };

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(args, CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.Args.Should().NotBeNullOrEmpty();
        addedTask.Args.Should().Contain("TestJob");
        addedTask.Args.Should().Contain("5");
        addedTask.Args.Should().Contain("true");
    }

    [Test]
    public async Task EnqueueAsync_ShouldCallSaveChanges()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable();

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(new { }, CancellationToken.None);

        // Assert
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EnqueueAsync_WithNullArgs_ShouldSerializeAsNull()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(null!, CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.Args.Should().Be("null");
    }

    [Test]
    public async Task EnqueueAsync_MultipleEnqueues_ShouldGenerateUniqueIds()
    {
        // Arrange
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var jobIds = new List<Guid>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var jobId = await queue.EnqueueAsync<TestBackgroundTask>(
                new { Index = i },
                CancellationToken.None
            );
            jobIds.Add(jobId);
        }

        // Assert
        jobIds.Should().HaveCount(10);
        jobIds.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region DequeueAsync Tests

    [Test]
    public async Task DequeueAsync_WhenTaskExists_ShouldReturnTask()
    {
        // Arrange
        var expectedTask = new BackgroundTask
        {
            Id = Guid.NewGuid(),
            Name = "TestTask",
            Args = "{}",
            Status = BackgroundTaskStatus.Enqueued
        };

        _mockBackgroundTaskDb
            .Setup(x => x.DequeueBackgroundTask())
            .Returns(expectedTask);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        var result = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedTask);
    }

    [Test]
    public async Task DequeueAsync_WhenNoTaskExists_ShouldReturnNull()
    {
        // Arrange
        _mockBackgroundTaskDb
            .Setup(x => x.DequeueBackgroundTask())
            .Returns((BackgroundTask?)null);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        var result = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task DequeueAsync_WhenExceptionOccurs_ShouldReturnNull()
    {
        // Arrange
        _mockBackgroundTaskDb
            .Setup(x => x.DequeueBackgroundTask())
            .Throws(new Exception("Database error"));

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        var result = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task DequeueAsync_ShouldCallBackgroundTaskDbDequeue()
    {
        // Arrange
        _mockBackgroundTaskDb
            .Setup(x => x.DequeueBackgroundTask())
            .Returns((BackgroundTask?)null)
            .Verifiable();

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);

        // Act
        await queue.DequeueAsync(CancellationToken.None);

        // Assert
        _mockBackgroundTaskDb.Verify(x => x.DequeueBackgroundTask(), Times.Once);
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public async Task DequeueAsync_ConcurrentCalls_ShouldBeSynchronized()
    {
        // Arrange
        var callCount = 0;
        var lockObject = new object();

        _mockBackgroundTaskDb
            .Setup(x => x.DequeueBackgroundTask())
            .Returns(() =>
            {
                lock (lockObject)
                {
                    callCount++;
                    Thread.Sleep(10); // Simulate some work
                    return new BackgroundTask
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Task_{callCount}",
                        Args = "{}",
                        Status = BackgroundTaskStatus.Enqueued
                    };
                }
            });

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var tasks = new List<Task<BackgroundTask>>();

        // Act - launch concurrent dequeue operations
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(queue.DequeueAsync(CancellationToken.None).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all calls should complete without exceptions
        results.Should().HaveCount(10);
        callCount.Should().Be(10);
    }

    [Test]
    public async Task EnqueueAsync_ConcurrentCalls_ShouldAllSucceed()
    {
        // Arrange
        var addedTasks = new List<BackgroundTask>();
        var lockObject = new object();

        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task =>
            {
                lock (lockObject)
                {
                    addedTasks.Add(task);
                }
            });

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var tasks = new List<Task<Guid>>();

        // Act - launch concurrent enqueue operations
        for (int i = 0; i < 20; i++)
        {
            var index = i;
            tasks.Add(queue.EnqueueAsync<TestBackgroundTask>(
                new { Index = index },
                CancellationToken.None
            ).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(20);
        results.Should().OnlyHaveUniqueItems();
        addedTasks.Should().HaveCount(20);
    }

    #endregion

    #region Complex Args Serialization Tests

    [Test]
    public async Task EnqueueAsync_WithNestedObject_ShouldSerializeCorrectly()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var args = new
        {
            User = new { Id = 1, Name = "John" },
            Items = new[] { "a", "b", "c" },
            Metadata = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(args, CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.Args.Should().Contain("John");
        addedTask.Args.Should().Contain("Items");
    }

    [Test]
    public async Task EnqueueAsync_WithDateTimeValue_ShouldSerializeCorrectly()
    {
        // Arrange
        BackgroundTask? addedTask = null;
        _mockBackgroundTaskSet
            .Setup(x => x.Add(It.IsAny<BackgroundTask>()))
            .Callback<BackgroundTask>(task => addedTask = task);

        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var queue = new BackgroundTaskQueue(_mockDbContext.Object, _mockBackgroundTaskDb.Object);
        var now = DateTime.UtcNow;
        var args = new { Timestamp = now };

        // Act
        await queue.EnqueueAsync<TestBackgroundTask>(args, CancellationToken.None);

        // Assert
        addedTask.Should().NotBeNull();
        addedTask!.Args.Should().NotBeNullOrEmpty();
        addedTask.Args.Should().Contain("Timestamp");
    }

    #endregion

    // Test helper class
    private class TestBackgroundTask { }
}

