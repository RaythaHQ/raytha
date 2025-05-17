using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.AuthenticationSchemes.Queries;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginWithEmailAndPassword : BaseAdminLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string returnUrl = null)
    {
        Form = new FormModel();
        var response = await Mediator.Send(
            new GetAuthenticationSchemes.Query
            {
                IsEnabledForAdmins = true,
                PageSize = int.MaxValue,
            }
        );

        AuthenticationSchemes = response.Result.Items.Select(
            p => new LoginAuthenticationSchemeChoiceItemViewModel
            {
                DeveloperName = p.DeveloperName,
                AuthenticationSchemeType = p.AuthenticationSchemeType,
                LoginButtonText = p.LoginButtonText,
            }
        );

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var singleSignOnScheme = response.Result.Items.First();
            return RedirectToPage(
                "/Login/LoginWithSso",
                new { developerName = singleSignOnScheme.DeveloperName, returnUrl }
            );
        }

        ViewData["returnUrl"] = returnUrl;
        if (BuiltInAuthIsMagicLinkOnly(response.Result))
            return RedirectToPage("/Login/LoginWithMagicLink", new { returnUrl });

        return Page();
    }

    public async Task<IActionResult> OnPost(string returnUrl = null)
    {
        var input = new Raytha.Application.Login.Commands.LoginWithEmailAndPassword.Command
        {
            EmailAddress = Form.EmailAddress,
            Password = Form.Password,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            await LoginWithClaims(response.Result, Form.RememberMe);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToPage("/Dashboard/Index");
            }
        }
        else
        {
            var authSchemes = await Mediator.Send(
                new GetAuthenticationSchemes.Query
                {
                    IsEnabledForAdmins = true,
                    PageSize = int.MaxValue,
                }
            );

            AuthenticationSchemes = authSchemes.Result.Items.Select(
                p => new LoginAuthenticationSchemeChoiceItemViewModel
                {
                    DeveloperName = p.DeveloperName,
                    AuthenticationSchemeType = p.AuthenticationSchemeType,
                    LoginButtonText = p.LoginButtonText,
                }
            );

            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Your email address")]
        public string EmailAddress { get; set; }

        [Display(Name = "Your password")]
        public string Password { get; set; }

        [Display(Name = "Keep me logged in")]
        public bool RememberMe { get; set; } = false;
    }
}
