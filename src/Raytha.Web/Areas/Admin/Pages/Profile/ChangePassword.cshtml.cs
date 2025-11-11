using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Profile;

public class ChangePassword : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Profile.Index);
        }

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Profile",
                RouteName = RouteNames.Profile.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Change password",
                RouteName = RouteNames.Profile.ChangePassword,
                IsActive = true,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Profile.Index);
        }

        var response = await Mediator.Send(
            new Raytha.Application.Login.Commands.ChangePassword.Command
            {
                Id = CurrentUser.UserId.Value,
                CurrentPassword = Form.CurrentPassword,
                NewPassword = Form.NewPassword,
                ConfirmNewPassword = Form.ConfirmNewPassword,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Password changed successfully.");
            return RedirectToPage(RouteNames.Profile.ChangePassword);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Your current password")]
        public string CurrentPassword { get; set; }

        [Display(Name = "Your new password")]
        public string NewPassword { get; set; }

        [Display(Name = "Re-type your new password")]
        public string ConfirmNewPassword { get; set; }
    }
}
