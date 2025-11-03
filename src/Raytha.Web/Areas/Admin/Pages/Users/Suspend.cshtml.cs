using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for suspending a user account.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Suspend : BaseAdminPageModel
{
    /// <summary>
    /// Handles POST requests to suspend a user account.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user edit page with success or error message.</returns>
    public async Task<IActionResult> OnPost(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var input = new SetIsActive.Command { Id = id, IsActive = false };
        var response = await Mediator.Send(input, cancellationToken);

        if (response.Success)
        {
            Logger.LogInformation("User {UserId} account suspended successfully", id);
            SetSuccessMessage($"Account has been suspended.");
        }
        else
        {
            Logger.LogWarning("Failed to suspend user {UserId}: {Error}", id, response.Error);
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToPage(RouteNames.Users.Edit, new { id });
    }
}
