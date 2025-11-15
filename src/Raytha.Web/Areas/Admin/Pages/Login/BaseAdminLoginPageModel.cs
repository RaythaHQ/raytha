using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Login;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Login;

[AllowAnonymous]
public class BaseAdminLoginPageModel : BaseAdminPageModel
{
    public bool ShowOrLoginWithSection => HasLoginByMagicLink || HasLoginBySingleSignOn;
    public bool HasLoginByEmailAndPassword =>
        AuthenticationSchemes.Any(p =>
            p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
        );
    public bool HasLoginByMagicLink =>
        AuthenticationSchemes.Any(p =>
            p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink
        );
    public bool HasLoginBySingleSignOn =>
        AuthenticationSchemes.Any(p =>
            p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt
            || p.AuthenticationSchemeType == AuthenticationSchemeType.Saml
        );

    public LoginAuthenticationSchemeChoiceItemViewModel EmailAndPassword =>
        HasLoginByEmailAndPassword
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
            )
            : null;
    public LoginAuthenticationSchemeChoiceItemViewModel MagicLink =>
        HasLoginByMagicLink
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItemViewModel> SingleSignOns =>
        HasLoginBySingleSignOn
            ? AuthenticationSchemes.Where(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt
                || p.AuthenticationSchemeType == AuthenticationSchemeType.Saml
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItemViewModel> AuthenticationSchemes { get; set; } =
        new List<LoginAuthenticationSchemeChoiceItemViewModel>();

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

    protected bool OnlyHasSingleSignOnEnabled(ListResultDto<AuthenticationSchemeDto> result)
    {
        return result.TotalCount == 1 && !result.Items.First().IsBuiltInAuth;
    }

    protected bool BuiltInAuthIsMagicLinkOnly(ListResultDto<AuthenticationSchemeDto> result)
    {
        return !result.Items.Any(p =>
                p.AuthenticationSchemeType
                == AuthenticationSchemeType.EmailAndPassword.DeveloperName
            )
            && result.Items.Any(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName
            );
    }

    protected bool BuiltInAuthIsEmailAndPasswordOnly(ListResultDto<AuthenticationSchemeDto> result)
    {
        return result.Items.Any(p =>
                p.AuthenticationSchemeType
                == AuthenticationSchemeType.EmailAndPassword.DeveloperName
            )
            && !result.Items.Any(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName
            );
    }
}

public record LoginAuthenticationSchemeChoiceItemViewModel
{
    public string AuthenticationSchemeType { get; init; }
    public string DeveloperName { get; init; }
    public string LoginButtonText { get; init; }
}
