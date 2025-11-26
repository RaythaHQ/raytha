namespace Raytha.Application.Common.Interfaces;

/// <summary>
/// Configuration settings for security-related features.
/// </summary>
public interface ISecurityConfiguration
{
    /// <summary>
    /// When true, allows internal/localhost URLs in server-side URL fetching operations
    /// (e.g., theme import from URL). Should only be enabled in development environments.
    /// </summary>
    bool AllowInternalUrlImports { get; }
}
