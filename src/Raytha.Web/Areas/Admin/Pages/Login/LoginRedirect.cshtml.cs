using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginRedirect : BaseAdminLoginPageModel
{
    public IActionResult OnGet(string returnUrl = null)
    {
        if (returnUrl == null || returnUrl.StartsWith($"{CurrentOrganization.PathBase}/raytha"))
        {
            return RedirectToSpaLogin(returnUrl);
        }

        return RedirectToPage(
            "/Login/LoginWithEmailAndPassword",
            new { area = "Public", returnUrl }
        );
    }
}
