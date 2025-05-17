using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.Login;

public class LoginAuthenticationSchemeChoiceItem_ViewModel
{
    public string AuthenticationSchemeType { get; init; }
    public string DeveloperName { get; init; }
    public string LoginButtonText { get; set; }
}

public class LoginWithEmailAndPassword_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Your email address")]
    public string EmailAddress { get; set; }

    [Display(Name = "Your password")]
    public string Password { get; set; }

    [Display(Name = "Keep me logged in")]
    public bool RememberMe { get; set; } = false;

    //helpers
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

    public LoginAuthenticationSchemeChoiceItem_ViewModel EmailAndPassword =>
        HasLoginByEmailAndPassword
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
            )
            : null;
    public LoginAuthenticationSchemeChoiceItem_ViewModel MagicLink =>
        HasLoginByMagicLink
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItem_ViewModel> SingleSignOns =>
        HasLoginBySingleSignOn
            ? AuthenticationSchemes.Where(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt
                || p.AuthenticationSchemeType == AuthenticationSchemeType.Saml
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItem_ViewModel> AuthenticationSchemes { get; init; } =
        new List<LoginAuthenticationSchemeChoiceItem_ViewModel>();
}

public class LoginWithMagicLink_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Your email address")]
    public string EmailAddress { get; set; }

    //helpers
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

    public LoginAuthenticationSchemeChoiceItem_ViewModel EmailAndPassword =>
        HasLoginByEmailAndPassword
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
            )
            : null;
    public LoginAuthenticationSchemeChoiceItem_ViewModel MagicLink =>
        HasLoginByMagicLink
            ? AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItem_ViewModel> SingleSignOns =>
        HasLoginBySingleSignOn
            ? AuthenticationSchemes.Where(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt
                || p.AuthenticationSchemeType == AuthenticationSchemeType.Saml
            )
            : null;
    public IEnumerable<LoginAuthenticationSchemeChoiceItem_ViewModel> AuthenticationSchemes { get; init; } =
        new List<LoginAuthenticationSchemeChoiceItem_ViewModel>();
}

public class BeginForgotPassword_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Your email address")]
    public string EmailAddress { get; set; }
}

public class CompleteForgotPassword_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }

    [Display(Name = "Your new password")]
    public string NewPassword { get; set; }

    [Display(Name = "Re-type your new password")]
    public string ConfirmNewPassword { get; set; }
}
