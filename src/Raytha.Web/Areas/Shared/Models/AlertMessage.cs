#nullable enable
namespace Raytha.Web.Areas.Shared.Models;

/// <summary>
/// Structured alert message for TempData serialization and display.
/// </summary>
public record AlertMessage
{
    /// <summary>
    /// Gets or sets the type of alert.
    /// </summary>
    public AlertType Type { get; init; } = AlertType.Info;

    /// <summary>
    /// Gets or sets the main message text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the optional alert title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets whether the alert can be dismissed by the user.
    /// </summary>
    public bool IsDismissible { get; init; } = true;
}

/// <summary>
/// Type of alert message.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Success message (green).
    /// </summary>
    Success,

    /// <summary>
    /// Error message (red).
    /// </summary>
    Error,

    /// <summary>
    /// Warning message (yellow).
    /// </summary>
    Warning,

    /// <summary>
    /// Informational message (blue).
    /// </summary>
    Info
}

