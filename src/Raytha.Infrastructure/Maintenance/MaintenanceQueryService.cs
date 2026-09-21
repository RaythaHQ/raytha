using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Maintenance;
using Raytha.Domain.Entities;

namespace Raytha.Infrastructure.Maintenance;

public sealed class MaintenanceQueryService : IMaintenanceQueryService
{
    private readonly IRaythaDbContext _db;
    private readonly IDbConnection _rawDb;
    private readonly IFileStorageProviderSettings _storageSettings;
    private readonly ICurrentVersion _currentVersion;
    private readonly IHostEnvironment _environment;

    public MaintenanceQueryService(
        IRaythaDbContext db,
        IDbConnection rawDb,
        IFileStorageProviderSettings storageSettings,
        ICurrentVersion currentVersion,
        IHostEnvironment environment
    )
    {
        _db = db;
        _rawDb = rawDb;
        _storageSettings = storageSettings;
        _currentVersion = currentVersion;
        _environment = environment;
    }

    public async Task<MaintenanceSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dbSizeBytes = await _rawDb.ExecuteScalarAsync<long>(
            "SELECT pg_database_size(current_database())"
        );

        var storageBytes = await _db.MediaItems.SumAsync(p => (long?)p.Length, cancellationToken) ?? 0;
        var storageCount = await _db.MediaItems.LongCountAsync(cancellationToken);

        var logs = new List<LogTableInfo>
        {
            new("audit_logs", "Audit logs", await _db.AuditLogs.LongCountAsync(cancellationToken)),
            new("email_logs", "Email log", await _db.EmailLogs.LongCountAsync(cancellationToken)),
            new(
                "webhook_deliveries",
                "Webhook deliveries",
                await _db.WebhookDeliveries.LongCountAsync(cancellationToken)
            ),
            new(
                "background_tasks",
                "Background tasks",
                await _db.BackgroundTasks.LongCountAsync(cancellationToken)
            ),
        };

        var taskCounts = await _db
            .BackgroundTasks.GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Count(BackgroundTaskStatus status) =>
            taskCounts
                .Where(c => c.Status.DeveloperName == status.DeveloperName)
                .Sum(c => c.Count);

        return new MaintenanceSnapshot(
            _currentVersion.Version,
            _environment.EnvironmentName,
            new DatabaseSizeInfo(
                dbSizeBytes,
                ByteSizeFormatter.Format(dbSizeBytes),
                _storageSettings.MaxTotalDbSize,
                ByteSizeFormatter.Format(_storageSettings.MaxTotalDbSize)
            ),
            new StorageSizeInfo(
                _storageSettings.FileStorageProvider,
                storageBytes,
                storageCount,
                ByteSizeFormatter.Format(storageBytes),
                _storageSettings.MaxTotalDiskSpace,
                ByteSizeFormatter.Format(_storageSettings.MaxTotalDiskSpace)
            ),
            logs,
            new BackgroundTaskCounts(
                Count(BackgroundTaskStatus.Enqueued),
                Count(BackgroundTaskStatus.Processing),
                Count(BackgroundTaskStatus.Complete),
                Count(BackgroundTaskStatus.Error)
            )
        );
    }
}
