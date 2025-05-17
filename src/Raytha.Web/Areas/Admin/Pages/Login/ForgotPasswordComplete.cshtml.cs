using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Login.Queries;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class ForgotPasswordComplete : BaseAdminLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string token)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme disabled for administrators");
            return new ForbidResult();
        }

        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Forgot password token is missing.");
            return new ForbidResult();
        }

        var isTokenValidResult = await Mediator.Send(
            new GetForgotPasswordTokenValidity.Query { Id = token }
        );
        if (!isTokenValidResult.Success)
        {
            SetErrorMessage(isTokenValidResult.Error);
            return new ForbidResult();
        }

        Form = new FormModel { Id = token };
        return Page();
    }

    public async Task<IActionResult> OnPost(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Forgot password token is missing.");
            return new ForbidResult();
        }

        var command = new Raytha.Application.Login.Commands.CompleteForgotPassword.Command
        {
            Id = token,
            NewPassword = Form.NewPassword,
            ConfirmNewPassword = Form.ConfirmNewPassword,
        };

        var response = await Mediator.Send(command);
        if (response.Success)
        {
            SetSuccessMessage("Password changed successfully. Please login.");
            return RedirectToPage("/Login/LoginWithEmailAndPassword");
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Your new password")]
        public string NewPassword { get; set; }

        [Display(Name = "Re-type your new password")]
        public string ConfirmNewPassword { get; set; }
    }
}
