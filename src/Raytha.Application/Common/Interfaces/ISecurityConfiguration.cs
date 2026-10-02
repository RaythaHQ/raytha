namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Configuration settings for security-related features.
/// </summary>
public interface ISecurityConfiguration
{
    /// <summary>
    /// When true, allows internal/localhost URLs in server-side requests: theme and CSV
    /// imports, webhook deliveries, and Raytha Functions HTTP calls.
    /// </summary>
    bool AllowInternalUrlImports { get; }
}
