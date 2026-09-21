namespace Raytha.Domain.Entities;

/// <summary>
/// A record of one outbound email send attempt, written by the email logging decorator.
/// </summary>
public class EmailLog : BaseEntity, IHasCreationTime
{
    public string ToAddress { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}
