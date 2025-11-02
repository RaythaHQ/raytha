using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Web.Areas.Admin.Pages.Shared;

namespace Raytha.Web.Areas.Admin.Pages.Login;

public class Logout : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage(RouteNames.Login.LoginWithEmailAndPassword, new { area = "Admin" });
    }
}
