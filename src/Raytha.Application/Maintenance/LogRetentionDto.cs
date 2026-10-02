namespace Raytha.Application.Maintenance;

/// <summary>Retention windows in days; 0 keeps entries forever.</summary>
public record LogRetentionDto
{
    public int AuditLogRetentionDays { get; init; }
    public int EmailLogRetentionDays { get; init; }
    public int WebhookDeliveryRetentionDays { get; init; }
    public int BackgroundTaskRetentionDays { get; init; }

    public static LogRetentionDto GetProjection(Domain.Entities.OrganizationSettings entity) =>
        new()
        {
            AuditLogRetentionDays = entity.AuditLogRetentionDays,
            EmailLogRetentionDays = entity.EmailLogRetentionDays,
            WebhookDeliveryRetentionDays = entity.WebhookDeliveryRetentionDays,
            BackgroundTaskRetentionDays = entity.BackgroundTaskRetentionDays,
        };
}
