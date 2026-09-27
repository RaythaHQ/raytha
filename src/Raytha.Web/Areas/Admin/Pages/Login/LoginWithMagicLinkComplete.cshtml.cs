using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Web.Areas.Admin.Pages.Shared;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginWithMagicLinkComplete : BaseAdminLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IActionResult OnGet(string returnUrl = null)
    {
        ViewData["returnUrl"] = returnUrl;
        Form = new FormModel { EmailAddress = TempData["MagicLinkEmailAddress"] as string };
        return Page();
    }

    public async Task<IActionResult> OnPost(string returnUrl = null)
    {
        var response = await Mediator.Send(
            new Raytha.Application.Login.Commands.CompleteLoginWithMagicLink.Command
            {
                EmailAddress = Form.EmailAddress ?? string.Empty,
                Code = Form.Code ?? string.Empty,
            }
        );

        if (response.Success)
        {
            await LoginWithClaims(response.Result, true);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToDashboard();
            }
        }
        else
        {
            ViewData["returnUrl"] = returnUrl;
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Your email address")]
        public string EmailAddress { get; set; }

        [Display(Name = "One-time code")]
        public string Code { get; set; }
    }
}
