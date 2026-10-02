using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;

namespace Raytha.Application.Maintenance;

/// <summary>
/// A table that grows without bound and is trimmed by a retention window. Retention and the
/// manual clear act on the same rows: every row for logs, finished rows for background tasks.
/// </summary>
public abstract class RetainedLog
{
    protected RetainedLog(string key, string label, Func<Domain.Entities.OrganizationSettings, int> retentionDays)
    {
        Key = key;
        Label = label;
        RetentionDays = retentionDays;
    }

    public string Key { get; }
    public string Label { get; }
    public Func<Domain.Entities.OrganizationSettings, int> RetentionDays { get; }

    /// <summary>Deletes retained rows older than <paramref name="olderThan"/>, or all of them when null.</summary>
    public abstract Task<int> DeleteAsync(IRaythaDbContext db, DateTime? olderThan, CancellationToken cancellationToken);

    public abstract Task<int> CountAsync(IRaythaDbContext db, DateTime? olderThan, CancellationToken cancellationToken);

    public abstract Task<DateTime?> OldestAsync(IRaythaDbContext db, CancellationToken cancellationToken);
}

public static class RetainedLogs
{
    public static readonly RetainedLog AuditLogs = new RetainedLog<AuditLog>(
        "audit_logs",
        "Audit log",
        s => s.AuditLogRetentionDays,
        db => db.AuditLogs,
        e => e.CreationTime
    );

    public static readonly RetainedLog EmailLogs = new RetainedLog<EmailLog>(
        "email_logs",
        "Email log",
        s => s.EmailLogRetentionDays,
        db => db.EmailLogs,
        e => e.CreationTime
    );

    public static readonly RetainedLog WebhookDeliveries = new RetainedLog<WebhookDelivery>(
        "webhook_deliveries",
        "Webhook deliveries",
        s => s.WebhookDeliveryRetentionDays,
        db => db.WebhookDeliveries,
        e => e.CreationTime
    );

    public static readonly RetainedLog BackgroundTasks = new RetainedLog<BackgroundTask>(
        "background_tasks",
        "Finished background tasks",
        s => s.BackgroundTaskRetentionDays,
        FinishedBackgroundTasks,
        e => e.CompletionTime ?? e.CreationTime
    );

    public static readonly IReadOnlyList<RetainedLog> All =
    [
        AuditLogs,
        EmailLogs,
        WebhookDeliveries,
        BackgroundTasks,
    ];

    public static RetainedLog? Find(string key) => All.FirstOrDefault(p => p.Key == key);

    /// <summary>The instant before which rows expire, or null when <paramref name="retentionDays"/> keeps rows forever.</summary>
    public static DateTime? Cutoff(int retentionDays, DateTime utcNow) =>
        retentionDays <= 0 ? null : utcNow.AddDays(-retentionDays);

    private static IQueryable<BackgroundTask> FinishedBackgroundTasks(IRaythaDbContext db)
    {
        var complete = BackgroundTaskStatus.Complete;
        var error = BackgroundTaskStatus.Error;
        return db.BackgroundTasks.Where(t => t.Status.Equals(complete) || t.Status.Equals(error));
    }
}

internal sealed class RetainedLog<TEntity> : RetainedLog
    where TEntity : class
{
    private readonly Func<IRaythaDbContext, IQueryable<TEntity>> _rows;
    private readonly Expression<Func<TEntity, DateTime>> _timestamp;

    public RetainedLog(
        string key,
        string label,
        Func<Domain.Entities.OrganizationSettings, int> retentionDays,
        Func<IRaythaDbContext, IQueryable<TEntity>> rows,
        Expression<Func<TEntity, DateTime>> timestamp
    )
        : base(key, label, retentionDays)
    {
        _rows = rows;
        _timestamp = timestamp;
    }

    // Log rows carry no domain events or interceptor-managed state, so a set-based delete is
    // safe and avoids loading millions of rows to trim a table.
    public override Task<int> DeleteAsync(
        IRaythaDbContext db,
        DateTime? olderThan,
        CancellationToken cancellationToken
    ) => Rows(db, olderThan).ExecuteDeleteAsync(cancellationToken);

    public override Task<int> CountAsync(
        IRaythaDbContext db,
        DateTime? olderThan,
        CancellationToken cancellationToken
    ) => Rows(db, olderThan).CountAsync(cancellationToken);

    public override Task<DateTime?> OldestAsync(IRaythaDbContext db, CancellationToken cancellationToken) =>
        _rows(db).AsNoTracking().Select(_timestamp).Select(t => (DateTime?)t).MinAsync(cancellationToken);

    private IQueryable<TEntity> Rows(IRaythaDbContext db, DateTime? olderThan)
    {
        var rows = _rows(db).AsNoTracking();
        if (olderThan is null)
        {
            return rows;
        }

        var cutoff = olderThan.Value;
        Expression<Func<DateTime>> cutoffParameter = () => cutoff;
        var olderThanCutoff = Expression.Lambda<Func<TEntity, bool>>(
            Expression.LessThan(_timestamp.Body, cutoffParameter.Body),
            _timestamp.Parameters
        );
        return rows.Where(olderThanCutoff);
    }
}
