using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Web.Areas.Admin.Pages.Shared;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class ForgotPassword : BaseAdminLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme disabled for administrators");
            return new ForbidResult();
        }
        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var response = await Mediator.Send(
            new Raytha.Application.Login.Commands.BeginForgotPassword.Command
            {
                EmailAddress = Form.EmailAddress,
            }
        );
        if (response.Success)
        {
            return RedirectToPage(RouteNames.Login.ForgotPasswordSent);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record FormModel
    {
        [Display(Name = "Your email address")]
        public string EmailAddress { get; set; }
    }
}
