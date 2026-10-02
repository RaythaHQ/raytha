using FluentAssertions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Maintenance;
using Raytha.Application.Maintenance.Commands;
using Raytha.Domain.Entities;
using OrganizationSettingsEntity = Raytha.Domain.Entities.OrganizationSettings;

namespace Raytha.Application.UnitTests.Maintenance;

public class RetainedLogsTests
{
    private static readonly DateTime Now = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Zero_days_keeps_entries_forever()
    {
        RetainedLogs.Cutoff(0, Now).Should().BeNull();
    }

    [Test]
    public void Cutoff_is_the_retention_window_before_now()
    {
        RetainedLogs.Cutoff(180, Now).Should().Be(Now.AddDays(-180));
        RetainedLogs.Cutoff(1, Now).Should().Be(Now.AddDays(-1));
    }

    [Test]
    public void Each_log_reads_its_own_retention_setting()
    {
        var settings = new OrganizationSettingsEntity
        {
            AuditLogRetentionDays = 1,
            EmailLogRetentionDays = 2,
            WebhookDeliveryRetentionDays = 3,
            BackgroundTaskRetentionDays = 4,
        };

        RetainedLogs.AuditLogs.RetentionDays(settings).Should().Be(1);
        RetainedLogs.EmailLogs.RetentionDays(settings).Should().Be(2);
        RetainedLogs.WebhookDeliveries.RetentionDays(settings).Should().Be(3);
        RetainedLogs.BackgroundTasks.RetentionDays(settings).Should().Be(4);
    }

    [Test]
    public void New_settings_default_every_window_to_180_days()
    {
        var settings = new OrganizationSettingsEntity();

        RetainedLogs.All.Select(log => log.RetentionDays(settings)).Should().AllBeEquivalentTo(180);
    }

    [Test]
    public void Log_keys_are_unique_and_resolvable()
    {
        RetainedLogs.All.Select(log => log.Key).Should().OnlyHaveUniqueItems();
        RetainedLogs.All.Should().AllSatisfy(log => RetainedLogs.Find(log.Key).Should().BeSameAs(log));
        RetainedLogs.Find("users").Should().BeNull();
    }

    [Test]
    public async Task Only_rows_older_than_the_cutoff_count_as_expired()
    {
        var db = new Mock<IRaythaDbContext>();
        var logs = new List<AuditLog>
        {
            new() { Id = Guid.NewGuid(), CreationTime = Now.AddDays(-200) },
            new() { Id = Guid.NewGuid(), CreationTime = Now.AddDays(-181) },
            new() { Id = Guid.NewGuid(), CreationTime = Now.AddDays(-179) },
            new() { Id = Guid.NewGuid(), CreationTime = Now },
        };
        db.Setup(x => x.AuditLogs).Returns(logs.AsQueryable().BuildMockDbSet().Object);

        var cutoff = RetainedLogs.Cutoff(180, Now);

        (await RetainedLogs.AuditLogs.CountAsync(db.Object, cutoff, CancellationToken.None)).Should().Be(2);
        (await RetainedLogs.AuditLogs.CountAsync(db.Object, null, CancellationToken.None)).Should().Be(4);
        (await RetainedLogs.AuditLogs.OldestAsync(db.Object, CancellationToken.None))
            .Should()
            .Be(Now.AddDays(-200));
    }

    [Test]
    public async Task Background_task_retention_only_covers_finished_tasks_by_completion_time()
    {
        var db = new Mock<IRaythaDbContext>();
        var old = Now.AddDays(-400);
        var tasks = new List<BackgroundTask>
        {
            new() { Status = BackgroundTaskStatus.Complete, CreationTime = old, CompletionTime = old },
            new() { Status = BackgroundTaskStatus.Error, CreationTime = old, CompletionTime = null },
            new() { Status = BackgroundTaskStatus.Complete, CreationTime = old, CompletionTime = Now },
            new() { Status = BackgroundTaskStatus.Processing, CreationTime = old },
            new() { Status = BackgroundTaskStatus.Enqueued, CreationTime = old },
        };
        db.Setup(x => x.BackgroundTasks).Returns(tasks.AsQueryable().BuildMockDbSet().Object);

        var cutoff = RetainedLogs.Cutoff(30, Now);

        (await RetainedLogs.BackgroundTasks.CountAsync(db.Object, cutoff, CancellationToken.None)).Should().Be(2);
        (await RetainedLogs.BackgroundTasks.CountAsync(db.Object, null, CancellationToken.None)).Should().Be(3);
    }

    [TestCase(0, true)]
    [TestCase(180, true)]
    [TestCase(3650, true)]
    [TestCase(-1, false)]
    [TestCase(3651, false)]
    public void Retention_accepts_zero_through_ten_years(int days, bool valid)
    {
        var command = new EditLogRetention.Command
        {
            AuditLogRetentionDays = days,
            EmailLogRetentionDays = 180,
            WebhookDeliveryRetentionDays = 180,
            BackgroundTaskRetentionDays = 180,
        };

        new EditLogRetention.Validator().Validate(command).IsValid.Should().Be(valid);
    }

    [Test]
    public void Clearing_an_unknown_log_is_rejected()
    {
        new ClearLog.Validator().Validate(new ClearLog.Command { Key = "users" }).IsValid.Should().BeFalse();
        new ClearLog.Validator().Validate(new ClearLog.Command { Key = "email_logs" }).IsValid.Should().BeTrue();
    }
}
