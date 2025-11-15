using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for restoring a suspended user account.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Restore : BaseAdminPageModel
{
    /// <summary>
    /// Handles POST requests to restore a suspended user account.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user edit page with success or error message.</returns>
    public async Task<IActionResult> OnPost(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Mediator.Send(
            new SetIsActive.Command { Id = id, IsActive = true },
            cancellationToken
        );

        if (response.Success)
        {
            Logger.LogInformation("User {UserId} account restored successfully", id);
            SetSuccessMessage($"Account has been restored.");
        }
        else
        {
            Logger.LogWarning("Failed to restore user {UserId}: {Error}", id, response.Error);
            SetErrorMessage(response.Error);
        }

        return RedirectToPage(RouteNames.Users.Edit, new { id });
    }
}
