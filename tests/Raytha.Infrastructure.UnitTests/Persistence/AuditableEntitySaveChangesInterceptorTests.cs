using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.Persistence;
using Raytha.Infrastructure.Persistence.Interceptors;

namespace Raytha.Infrastructure.UnitTests.Persistence;

[TestFixture]
public class AuditableEntitySaveChangesInterceptorTests
{
    [Test]
    public void ProgressSave_RenewsBackgroundTaskLease()
    {
        using var db = new RaythaDbContext(
            new DbContextOptionsBuilder<RaythaDbContext>().UseNpgsql("Host=unused").Options
        );
        var claimedAt = DateTime.UtcNow.AddMinutes(-5);
        var task = new BackgroundTask
        {
            Id = Guid.NewGuid(),
            Name = "job",
            Status = BackgroundTaskStatus.Processing,
            LastModificationTime = claimedAt,
        };
        db.BackgroundTasks.Attach(task);

        task.PercentComplete = 40;
        db.BackgroundTasks.Update(task);
        new AuditableEntitySaveChangesInterceptor(Mock.Of<ICurrentUser>()).UpdateEntities(db);

        task.LastModificationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
