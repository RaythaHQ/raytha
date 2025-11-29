namespace Raytha.Domain.Entities;

public class FailedLoginAttempt : BaseEntity
{
    public string EmailAddress { get; set; } = null!;
    public int FailedAttemptCount { get; set; }
    public DateTime LastFailedAttemptAt { get; set; }
}

