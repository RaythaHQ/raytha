namespace Raytha.Domain.Entities;

public class OrganizationSettings : BaseEntity
{
    public string? OrganizationName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public bool SmtpOverrideSystem { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpDefaultFromAddress { get; set; }
    public string? SmtpDefaultFromName { get; set; }
    public Guid? HomePageId { get; set; }
    public string HomePageType { get; set; } = Route.CONTENT_ITEM_TYPE;
    public Guid ActiveThemeId { get; set; }

    public const int DEFAULT_RETENTION_DAYS = 180;
    public const int MAX_RETENTION_DAYS = 3650;

    /// <summary>Retention windows in days; 0 keeps entries forever.</summary>
    public int AuditLogRetentionDays { get; set; } = DEFAULT_RETENTION_DAYS;
    public int EmailLogRetentionDays { get; set; } = DEFAULT_RETENTION_DAYS;
    public int WebhookDeliveryRetentionDays { get; set; } = DEFAULT_RETENTION_DAYS;
    public int BackgroundTaskRetentionDays { get; set; } = DEFAULT_RETENTION_DAYS;
}
