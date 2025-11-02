using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class ResetPassword : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Admins.Edit, new { id });
        }

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false
            },
            new BreadcrumbNode
            {
                Label = "Admins",
                RouteName = RouteNames.Admins.Index,
                IsActive = false
            },
            new BreadcrumbNode
            {
                Label = "Reset Password",
                RouteName = RouteNames.Admins.ResetPassword,
                IsActive = true
            }
        );

        var response = await Mediator.Send(new GetAdminById.Query { Id = id });

        Form = new FormModel
        {
            Id = id
        };

        Id = id;
        IsActive = response.Result.IsActive;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new Raytha.Application.Admins.Commands.ResetPassword.Command
        {
            Id = Form.Id,
            ConfirmNewPassword = Form.ConfirmNewPassword,
            NewPassword = Form.NewPassword,
            SendEmail = Form.SendEmail,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Password was reset successfully.");
            return RedirectToPage(RouteNames.Admins.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to reset this password. See the error below.",
                response.GetErrors()
            );
            var admin = await Mediator.Send(new GetAdminById.Query { Id = id });
            Id = id;
            IsActive = admin.Result.IsActive;
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
