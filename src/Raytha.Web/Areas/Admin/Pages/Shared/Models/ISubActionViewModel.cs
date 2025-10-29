namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

/// <summary>
/// Interface for page models that support sub-actions (e.g., edit, delete, suspend).
/// Used by layout pages to render contextual navigation and actions.
/// </summary>
public interface ISubActionViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity being acted upon.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Gets or sets whether the entity is currently active.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether the entity is an administrator.
    /// Used to disable certain actions for admin users.
    /// </summary>
    bool IsAdmin { get; set; }
}
