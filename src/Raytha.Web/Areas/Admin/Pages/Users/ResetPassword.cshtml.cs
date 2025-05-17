using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class ResetPassword : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public string Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage("/Users/Edit", new { id });
        }

        var response = await Mediator.Send(new GetUserById.Query { Id = id });

        if (response.Result.IsAdmin)
        {
            SetErrorMessage("You cannot reset an administrator's password from this screen.");
            return RedirectToPage("/Users/Edit", new { id });
        }

        Form = new FormModel();
        Id = id;
        IsActive = response.Result.IsActive;
        IsAdmin = response.Result.IsAdmin;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new Raytha.Application.Users.Commands.ResetPassword.Command
        {
            Id = id,
            ConfirmNewPassword = Form.ConfirmNewPassword,
            NewPassword = Form.NewPassword,
            SendEmail = Form.SendEmail,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Password was reset successfully.");
            return RedirectToPage("/Users/Edit", new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to reset this password. See the error below.",
                response.GetErrors()
            );
            var user = await Mediator.Send(new GetUserById.Query { Id = id });
            Id = id;
            IsAdmin = user.Result.IsAdmin;
            IsActive = user.Result.IsActive;

            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [Display(Name = "Re-type the new password")]
        public string ConfirmNewPassword { get; set; }

        public bool SendEmail { get; set; } = true;
    }
}
