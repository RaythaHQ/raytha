using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for creating a new user.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    /// <summary>
    /// Gets or sets the form model for user creation.
    /// </summary>
    [BindProperty]
    public FormModel Form { get; set; } = new();

    /// <summary>
    /// Handles GET requests to display the user creation form.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The page result.</returns>
    public async Task<IActionResult> OnGet(CancellationToken cancellationToken = default)
    {
        var userGroupsChoicesResponse = await Mediator.Send(new GetUserGroups.Query(), cancellationToken);
        var userGroupsViewModel = userGroupsChoicesResponse
            .Result.Items.Select(p => new CheckboxItemViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = false,
            })
            .ToArray();

        Form = new FormModel { UserGroups = userGroupsViewModel };

        Logger.LogInformation("Displaying user creation form");

        return Page();
    }

    /// <summary>
    /// Handles POST requests to create a new user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>Redirect to user index on success, or page with errors on failure.</returns>
    public async Task<IActionResult> OnPost(CancellationToken cancellationToken = default)
    {
        var input = new CreateUser.Command
        {
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            UserGroups = Form.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
            SendEmail = Form.SendEmail,
        };

        var response = await Mediator.Send(input, cancellationToken);

        if (response.Success)
        {
            Logger.LogInformation(
                "User created successfully: {EmailAddress} ({FirstName} {LastName})",
                Form.EmailAddress,
                Form.FirstName,
                Form.LastName
            );
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was created successfully.");
            return RedirectToPage(RouteNames.Users.Index);
        }

        Logger.LogWarning(
            "Failed to create user {EmailAddress}: {Errors}",
            Form.EmailAddress,
            string.Join("; ", response.GetErrors().Select(e => e.ErrorMessage))
        );
        SetErrorMessage(
            "There was an error attempting to create this user. See the error below.",
            response.GetErrors()
        );
        return Page();
    }

    /// <summary>
    /// Form model for user creation.
    /// </summary>
    public record FormModel
    {
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
        /// Gets or sets whether to send a welcome email to the new user.
        /// </summary>
        [Display(Name = "Send admin welcome email")]
        public bool SendEmail { get; set; } = true;

        /// <summary>
        /// Gets or sets the user groups that the user can belong to.
        /// </summary>
        public CheckboxItemViewModel[]? UserGroups { get; set; }
    }
}