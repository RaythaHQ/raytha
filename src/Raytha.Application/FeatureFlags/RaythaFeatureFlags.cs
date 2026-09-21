namespace Raytha.Application.FeatureFlags;

public record FeatureFlagDefinition(
    string Key,
    string Label,
    string Description,
    bool DefaultEnabled
);

/// <summary>
/// The code-defined catalog of feature flags. Only keys listed here can be evaluated or
/// overridden; anything else fails closed.
/// </summary>
public static class RaythaFeatureFlags
{
    public const string Webhooks = "webhooks";
    public const string EmailLog = "email_log";
    public const string AdminSpa = "admin_spa";
    public const string MaintenanceTools = "maintenance_tools";

    public static readonly IReadOnlyList<FeatureFlagDefinition> All = new[]
    {
        new FeatureFlagDefinition(
            Webhooks,
            "Webhooks",
            "Publish outbound webhooks when annotated commands succeed.",
            DefaultEnabled: true
        ),
        new FeatureFlagDefinition(
            EmailLog,
            "Email log",
            "Record every outbound email attempt (subject, recipient, outcome) for troubleshooting.",
            DefaultEnabled: true
        ),
        new FeatureFlagDefinition(
            AdminSpa,
            "Admin SPA",
            "Serve the new single-page admin experience instead of the Razor admin.",
            DefaultEnabled: false
        ),
        new FeatureFlagDefinition(
            MaintenanceTools,
            "Maintenance tools",
            "Expose maintenance actions (clear logs, probe background tasks) in the admin.",
            DefaultEnabled: true
        ),
    };

    public static FeatureFlagDefinition? Find(string key)
    {
        return All.FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase)
        );
    }
}
