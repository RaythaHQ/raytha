namespace Raytha.Application.Maintenance;

public interface IMaintenanceQueryService
{
    Task<MaintenanceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record MaintenanceSnapshot(
    string Version,
    string Environment,
    DatabaseSizeInfo Database,
    StorageSizeInfo Storage,
    IReadOnlyList<LogTableInfo> Logs,
    BackgroundTaskCounts BackgroundTasks
);

public sealed record DatabaseSizeInfo(
    long SizeBytes,
    string SizeDisplay,
    long MaxBytes,
    string MaxDisplay
);

public sealed record StorageSizeInfo(
    string Provider,
    long SizeBytes,
    long FileCount,
    string SizeDisplay,
    long MaxBytes,
    string MaxDisplay
);

public sealed record LogTableInfo(string Key, string Label, long RowCount);

public sealed record BackgroundTaskCounts(int Enqueued, int Processing, int Complete, int Error);

public static class ByteSizeFormatter
{
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{value:0.#} {Units[unit]}";
    }
}
