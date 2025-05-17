namespace Raytha.Domain.Entities;

public class BackgroundTask : BaseEntity, IHasCreationTime, IHasModificationTime
{
    public string Name { get; set; }
    public string? Args { get; set; }
    public string? ErrorMessage { get; set; }
    public BackgroundTaskStatus Status { get; set; } = BackgroundTaskStatus.Enqueued;
    public string? StatusInfo { get; set; }
    public int PercentComplete { get; set; }
    public int NumberOfRetries { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastModificationTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int TaskStep { get; set; }
}

public class BackgroundTaskStatus : ValueObject
{
    static BackgroundTaskStatus() { }

    public BackgroundTaskStatus() { }

    private BackgroundTaskStatus(string label, string developerName)
    {
        Label = label;
        DeveloperName = developerName;
    }

    public static BackgroundTaskStatus From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName.ToLower());

        if (type == null)
        {
            throw new SortOrderNotFoundException(developerName);
        }

        return type;
    }

    public static BackgroundTaskStatus Enqueued => new("Enqueued", "enqueued");
    public static BackgroundTaskStatus Processing => new("Processing", "processing");
    public static BackgroundTaskStatus Complete => new("Complete", "complete");
    public static BackgroundTaskStatus Error => new("Error", "error");

    public string Label { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;

    public static implicit operator string(BackgroundTaskStatus scheme)
    {
        return scheme.DeveloperName;
    }

    public static explicit operator BackgroundTaskStatus(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return Label;
    }

    public static IEnumerable<BackgroundTaskStatus> SupportedTypes
    {
        get
        {
            yield return Enqueued;
            yield return Processing;
            yield return Complete;
            yield return Error;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
