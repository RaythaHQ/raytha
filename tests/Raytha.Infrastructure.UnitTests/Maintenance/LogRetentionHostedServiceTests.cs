using FluentAssertions;
using Raytha.Infrastructure.Maintenance;

namespace Raytha.Infrastructure.UnitTests.Maintenance;

public class LogRetentionHostedServiceTests
{
    [Test]
    public void Before_three_am_utc_the_next_run_is_today()
    {
        var now = new DateTimeOffset(2026, 9, 26, 1, 30, 0, TimeSpan.Zero);

        LogRetentionHostedService.DelayUntilNextRun(now).Should().Be(TimeSpan.FromMinutes(90));
    }

    [Test]
    public void At_or_after_three_am_utc_the_next_run_is_tomorrow()
    {
        var atThree = new DateTimeOffset(2026, 9, 26, 3, 0, 0, TimeSpan.Zero);
        var evening = new DateTimeOffset(2026, 9, 26, 22, 0, 0, TimeSpan.Zero);

        LogRetentionHostedService.DelayUntilNextRun(atThree).Should().Be(TimeSpan.FromDays(1));
        LogRetentionHostedService.DelayUntilNextRun(evening).Should().Be(TimeSpan.FromHours(5));
    }

    [Test]
    public void Local_offsets_are_scheduled_against_utc()
    {
        var chicagoEvening = new DateTimeOffset(2026, 9, 26, 21, 0, 0, TimeSpan.FromHours(-5));

        LogRetentionHostedService.DelayUntilNextRun(chicagoEvening).Should().Be(TimeSpan.FromHours(1));
    }
}
