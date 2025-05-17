using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginWithJwt : BaseAdminLoginPageModel
{
    public async Task<IActionResult> OnGet(
        string developerName,
        string token,
        string returnUrl = null
    )
    {
        var command = new Raytha.Application.Login.Commands.LoginWithJwt.Command
        {
            DeveloperName = developerName,
            Token = token,
        };
        var response = await Mediator.Send(command);
        if (response.Success)
        {
            await LoginWithClaims(response.Result, true);
            if (HasLocalRedirect(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToPage("/Dashboard/Index");
        }
        else
        {
            throw new UnauthorizedAccessException(response.Error);
        }
    }
}
