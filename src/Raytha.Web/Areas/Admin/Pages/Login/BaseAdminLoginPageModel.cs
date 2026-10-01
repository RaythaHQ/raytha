using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Security;
using Raytha.Application.Login;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Login;

/// <summary>
/// Server-side sign-in endpoints: SSO entry and the JWT and SAML callbacks. The interactive
/// sign-in screens are SPA routes under <c>/raytha/login</c>.
/// </summary>
[AllowAnonymous]
public class BaseAdminLoginPageModel : BaseAdminPageModel
{
    protected async Task LoginWithClaims(LoginDto result, bool rememberMe = true)
    {
        List<Claim> claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()));
        claims.Add(
            new Claim(RaythaClaimTypes.LastModificationTime, result.LastModificationTime.ToString())
        );
        ClaimsIdentity identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );
        ClaimsPrincipal principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(principal),
            new AuthenticationProperties() { IsPersistent = rememberMe }
        );
    }

    protected bool HasLocalRedirect(string returnUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl);
    }

    /// <summary>
    /// The admin dashboard is served by the React SPA at /raytha, not a Razor page.
    /// </summary>
    protected IActionResult RedirectToDashboard()
    {
        return Redirect($"{CurrentOrganization.PathBase}/raytha");
    }

    protected IActionResult RedirectToSpaLogin(string returnUrl = null)
    {
        var query = HasLocalRedirect(returnUrl)
            ? new QueryBuilder { { "returnUrl", returnUrl } }.ToString()
            : string.Empty;
        return Redirect($"{CurrentOrganization.PathBase}/raytha/login{query}");
    }
}
