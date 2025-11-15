using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for deleting a user.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    /// <summary>
    /// Handles POST requests to delete a user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user index with success or error message.</returns>
    public async Task<IActionResult> OnPost(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Mediator.Send(new DeleteUser.Command { Id = id }, cancellationToken);

        if (response.Success)
        {
            Logger.LogInformation("User {UserId} deleted successfully", id);
            SetSuccessMessage($"User has been deleted.");
        }
        else
        {
            Logger.LogWarning("Failed to delete user {UserId}: {Error}", id, response.Error);
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(RouteNames.Users.Index);
    }
}
