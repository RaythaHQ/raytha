#nullable enable
namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Model for confirmation dialog modal.
/// </summary>
public class ConfirmActionModel
{
    /// <summary>
    /// Gets or sets the unique ID for the modal (e.g., "confirm-delete").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the modal title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the confirmation message to display.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets the text for the confirm button.
    /// </summary>
    public required string ConfirmButtonText { get; init; }

    /// <summary>
    /// Gets or sets the CSS class for the confirm button (e.g., "btn-danger").
    /// </summary>
    public string ConfirmButtonClass { get; init; } = "btn-primary";

    /// <summary>
    /// Gets or sets the text for the cancel button.
    /// </summary>
    public string? CancelButtonText { get; init; }

    /// <summary>
    /// Gets or sets whether to show a warning alert in the modal.
    /// </summary>
    public bool ShowWarning { get; init; } = true;

    /// <summary>
    /// Gets or sets the form action URL (can be set via data attribute on trigger button).
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Gets or sets additional route values to include as hidden inputs.
    /// </summary>
    public Dictionary<string, string>? RouteValues { get; init; }
}

