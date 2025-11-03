using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for resetting a user's password.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class ResetPassword : BaseAdminPageModel, ISubActionViewModel
{
    /// <summary>
    /// Gets or sets the form model for password reset.
    /// </summary>
    [BindProperty]
    public FormModel Form { get; set; } = new();

    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether the user is an administrator.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Handles GET requests to display the password reset form.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The page result or redirect if operation is not allowed.</returns>
    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken = default)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            Logger.LogWarning(
                "Attempted to reset password for user {UserId} but email/password authentication is disabled",
                id
            );
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }

        var response = await Mediator.Send(new GetUserById.Query { Id = id }, cancellationToken);

        if (response.Result.IsAdmin)
        {
            Logger.LogWarning(
                "Attempted to reset password for admin user {UserId} from user management screen",
                id
            );
            SetErrorMessage("You cannot reset an administrator's password from this screen.");
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                Icon = SidebarIcons.Users,
            },
            new BreadcrumbNode
            {
                Label = $"{response.Result.FirstName} {response.Result.LastName}",
                RouteName = RouteNames.Users.Edit,
                RouteValues = new Dictionary<string, string> { { "id", id } },
            },
            new BreadcrumbNode
            {
                Label = "Reset Password",
                RouteName = RouteNames.Users.ResetPassword,
                RouteValues = new Dictionary<string, string> { { "id", id } },
                IsActive = true,
            }
        );

        Form = new FormModel();
        Id = id;
        IsActive = response.Result.IsActive;
        IsAdmin = response.Result.IsAdmin;

        Logger.LogInformation("Displaying password reset form for user {UserId}", id);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to reset a user's password.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user edit page on success, or page with errors on failure.</returns>
    public async Task<IActionResult> OnPost(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        var input = new Raytha.Application.Users.Commands.ResetPassword.Command
        {
            Id = id,
            ConfirmNewPassword = Form.ConfirmNewPassword,
            NewPassword = Form.NewPassword,
            SendEmail = Form.SendEmail,
        };
        var response = await Mediator.Send(input, cancellationToken);

        if (response.Success)
        {
            Logger.LogInformation(
                "Password reset successfully for user {UserId}, SendEmail: {SendEmail}",
                id,
                Form.SendEmail
            );
            SetSuccessMessage($"Password was reset successfully.");
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }

        Logger.LogWarning(
            "Failed to reset password for user {UserId}: {Errors}",
            id,
            string.Join("; ", response.GetErrors().Select(e => e.ErrorMessage))
        );
        SetErrorMessage(
            "There was an error attempting to reset this password. See the error below.",
            response.GetErrors()
        );

        // Preserve the display properties for the layout without re-querying
        Id = id;

        return Page();
    }

    /// <summary>
    /// Form model for password reset.
    /// </summary>
    public record FormModel
    {
        /// <summary>
        /// Gets or sets the user's unique identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password confirmation.
        /// </summary>
        [Display(Name = "Re-type the new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to send an email notification about the password reset.
        /// </summary>
        public bool SendEmail { get; set; } = true;
    }
}
