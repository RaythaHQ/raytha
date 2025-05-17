using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class LoginRedirect : BaseAdminLoginPageModel
{
    public async Task<IActionResult> OnGet(string returnUrl = null)
    {
        if (returnUrl.StartsWith($"{CurrentOrganization.PathBase}/admin"))
        {
            return RedirectToPage(
                "/Login/LoginWithEmailAndPassword",
                new { area = "Admin", returnUrl }
            );
        }
        else
        {
            return RedirectToPage(
                "/Login/LoginWithEmailAndPassword",
                new { area = "Public", returnUrl }
            );
        }
    }
}
