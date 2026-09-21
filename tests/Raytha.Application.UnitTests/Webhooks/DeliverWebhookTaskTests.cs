using FluentAssertions;
using Raytha.Application.Webhooks;

namespace Raytha.Application.UnitTests.Webhooks;

public class DeliverWebhookTaskTests
{
    [TestCase(1, 2)]
    [TestCase(2, 4)]
    [TestCase(3, 8)]
    [TestCase(4, 16)]
    [TestCase(5, 32)]
    public void ComputeBackoff_DoublesEachAttempt(int attempt, int expectedSeconds)
    {
        DeliverWebhookTask.ComputeBackoff(attempt).Should().Be(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Test]
    public void ComputeBackoff_IsCappedAtSixtySeconds()
    {
        DeliverWebhookTask.ComputeBackoff(6).Should().Be(TimeSpan.FromSeconds(60));
        DeliverWebhookTask.ComputeBackoff(50).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Test]
    public void ComputeBackoff_TreatsNonPositiveAttemptsAsFirst()
    {
        DeliverWebhookTask.ComputeBackoff(0).Should().Be(TimeSpan.FromSeconds(2));
        DeliverWebhookTask.ComputeBackoff(-3).Should().Be(TimeSpan.FromSeconds(2));
    }
}
