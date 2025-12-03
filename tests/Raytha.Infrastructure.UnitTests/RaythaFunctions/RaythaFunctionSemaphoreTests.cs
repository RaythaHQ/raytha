using FluentAssertions;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Infrastructure.RaythaFunctions;

namespace Raytha.Infrastructure.UnitTests.RaythaFunctions;

[TestFixture]
public class RaythaFunctionSemaphoreTests
{
    #region Constructor Tests

    [Test]
    public void Constructor_ShouldInitializeWithMaxActive()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(5);

        // Act
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Assert
        semaphore.CurrentCount.Should().Be(5);
    }

    [Test]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(100)]
    [Parallelizable(ParallelScope.All)]
    public void Constructor_WithVariousMaxActive_ShouldSetCorrectCount(int maxActive)
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(maxActive);

        // Act
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Assert
        semaphore.CurrentCount.Should().Be(maxActive);
    }

    #endregion

    #region WaitAsync Tests

    [Test]
    public async Task WaitAsync_WhenSlotsAvailable_ShouldReturnTrue()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(5);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Act
        var result = await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        semaphore.CurrentCount.Should().Be(4);
    }

    [Test]
    public async Task WaitAsync_ShouldDecrementAvailableSlots()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(3);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Act
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        semaphore.CurrentCount.Should().Be(1);
    }

    [Test]
    public async Task WaitAsync_WhenNoSlotsAvailable_ShouldReturnFalseOnTimeout()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(1);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Take the only available slot
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        // Act
        var result = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(50), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task WaitAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(1);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Take the only available slot
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await FluentActions
            .Invoking(async () => await semaphore.WaitAsync(TimeSpan.FromSeconds(10), cts.Token))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Release Tests

    [Test]
    public async Task Release_ShouldIncrementAvailableSlots()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(3);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        semaphore.CurrentCount.Should().Be(1);

        // Act
        semaphore.Release();

        // Assert
        semaphore.CurrentCount.Should().Be(2);
    }

    [Test]
    public async Task Release_ShouldReturnPreviousCount()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(5);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);
        // Current count is now 3

        // Act
        var previousCount = semaphore.Release();

        // Assert
        previousCount.Should().Be(3);
        semaphore.CurrentCount.Should().Be(4);
    }

    [Test]
    public async Task Release_ShouldAllowWaitingThreadsToProceed()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(1);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        await semaphore.WaitAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

        var waitTask = Task.Run(async () =>
        {
            await Task.Delay(50);
            return await semaphore.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        });

        // Act
        await Task.Delay(100);
        semaphore.Release();
        var result = await waitTask;

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Thread Safety Tests

    [Test]
    public async Task Semaphore_ShouldLimitConcurrentAccess()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(3);
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockObj = new object();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                if (await semaphore.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None))
                {
                    try
                    {
                        lock (lockObj)
                        {
                            currentConcurrent++;
                            maxConcurrent = Math.Max(maxConcurrent, currentConcurrent);
                        }

                        await Task.Delay(50);
                    }
                    finally
                    {
                        lock (lockObj)
                        {
                            currentConcurrent--;
                        }
                        semaphore.Release();
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrent.Should().Be(3, "only 3 concurrent executions should be allowed");
    }

    #endregion

    #region IRaythaFunctionSemaphore Interface Tests

    [Test]
    public void ShouldImplementIRaythaFunctionSemaphore()
    {
        // Arrange
        var mockConfig = new Mock<IRaythaFunctionConfiguration>();
        mockConfig.Setup(x => x.MaxActive).Returns(1);

        // Act
        using var semaphore = new RaythaFunctionSemaphore(mockConfig.Object);

        // Assert
        semaphore.Should().BeAssignableTo<IRaythaFunctionSemaphore>();
    }

    #endregion
}

