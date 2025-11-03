using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for editing an existing user.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    /// <summary>
    /// Gets or sets the form model for user editing.
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
    /// Handles GET requests to display the user editing form.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken = default)
    {
        var response = await Mediator.Send(new GetUserById.Query { Id = id }, cancellationToken);

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                Icon = SidebarIcons.Users
            },
            new BreadcrumbNode
            {
                Label = $"{response.Result.FirstName} {response.Result.LastName}",
                RouteName = RouteNames.Users.Edit,
                RouteValues = new Dictionary<string, string> { { "id", id } },
                IsActive = true
            }
        );

        var allUserGroups = await Mediator.Send(new GetUserGroups.Query(), cancellationToken);
        var userGroups = allUserGroups.Result.Items.Select(
            p => new CheckboxItemViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = response.Result.UserGroups.Select(u => u.Id).Contains(p.Id),
            }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            FirstName = response.Result.FirstName,
            LastName = response.Result.LastName,
            EmailAddress = response.Result.EmailAddress,
            UserGroups = userGroups.ToArray(),
        };

        Id = id;
        IsActive = response.Result.IsActive;
        IsAdmin = response.Result.IsAdmin;

        Logger.LogInformation("Displaying user edit form for user {UserId} ({EmailAddress})", id, response.Result.EmailAddress);

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update an existing user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user edit page on success, or page with errors on failure.</returns>
    public async Task<IActionResult> OnPost(string id, CancellationToken cancellationToken = default)
    {
        var input = new EditUser.Command
        {
            Id = id,
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            UserGroups = Form.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
        };

        var response = await Mediator.Send(input, cancellationToken);

        if (response.Success)
        {
            Logger.LogInformation(
                "User updated successfully: {UserId} - {EmailAddress} ({FirstName} {LastName})",
                id,
                Form.EmailAddress,
                Form.FirstName,
                Form.LastName
            );
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was updated successfully.");
            return RedirectToPage(RouteNames.Users.Edit, new { id });
        }

        // On error, preserve the current state without re-querying
        Logger.LogWarning(
            "Failed to update user {UserId}: {Errors}",
            id,
            string.Join("; ", response.GetErrors().Select(e => e.ErrorMessage))
        );
        SetErrorMessage(
            "There was an error attempting to update this user. See the error below.",
            response.GetErrors()
        );

        // Preserve the display properties for the layout
        Id = id;
        // Note: IsAdmin and IsActive are not in the response, so we need to keep them from the form state
        // or accept that they may be default values in the error case
        return Page();
    }

    /// <summary>
    /// Form model for user editing.
    /// </summary>
    public record FormModel
    {
        /// <summary>
        /// Gets or sets the user's unique identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user groups that the user can belong to.
        /// </summary>
        public CheckboxItemViewModel[]? UserGroups { get; set; }
    }
}