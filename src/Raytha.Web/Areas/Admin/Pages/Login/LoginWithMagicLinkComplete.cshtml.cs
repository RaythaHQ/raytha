using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginWithMagicLinkComplete : BaseAdminLoginPageModel
{
    public async Task<IActionResult> OnGet(string token = null, string returnUrl = null)
    {
        if (string.IsNullOrEmpty(token))
        {
            SetErrorMessage("Login token is missing.");
            return new ForbidResult();
        }

        var response = await Mediator.Send(
            new Raytha.Application.Login.Commands.CompleteLoginWithMagicLink.Command { Id = token }
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
                return RedirectToPage("/Dashboard/Index");
            }
        }
        else
        {
            SetErrorMessage(response.Error);
            return RedirectToPage("/Login/LoginWithMagicLink", new { returnUrl });
        }
    }
}
