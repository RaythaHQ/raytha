using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Web.Areas.Admin.Views.Shared;

namespace Raytha.Web.Areas.Admin.Views.AuthenticationSchemes;

public class AuthenticationSchemesListItem_ViewModel
{
    public string Id { get; init; }

    [Display(Name = "Label")]
    public string Label { get; init; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModificationTime { get; init; }

    [Display(Name = "Last modified at")]
    public string LastModifierUser { get; init; }

    [Display(Name = "Auth scheme type")]
    public string AuthenticationSchemeType { get; init; }

    [Display(Name = "Is enabled for public users")]
    public string IsEnabledForUsers { get; set; }

    [Display(Name = "Is enabled for admins")]
    public string IsEnabledForAdmins { get; set; }
}

public class AuthenticationSchemesCreate_ViewModel : FormSubmit_ViewModel
{
    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Authentication scheme type")]
    public string AuthenticationSchemeType { get; set; }

    [Display(Name = "Is enabled for public users")]
    public bool IsEnabledForUsers { get; set; }

    [Display(Name = "Is enabled for admins")]
    public bool IsEnabledForAdmins { get; set; }

    [Display(Name = "Certificate (x.509)")]
    public string SamlCertificate { get; set; }

    [Display(Name = "Secret key")]
    public string JwtSecretKey { get; set; }

    [Display(Name = "Sign-in url")]
    public string SignInUrl { get; set; }

    [Display(Name = "Login button text")]
    public string LoginButtonText { get; set; }

    [Display(Name = "Sign-out url")]
    public string SignOutUrl { get; set; }

    [Display(Name = "Magic link expires after this amount of seconds")]
    public int MagicLinkExpiresInSeconds { get; set; } = 900;
    
    [Display(Name = "Use added security (implement 'jti' claim)")]
    public bool JwtUseHighSecurity { get; set; } = false;
    
    [Display(Name = "IdP entity id")]
    public string SamlIdpEntityId { get; set; }

    //helpers
    public SelectList SupportedAuthenticationSchemeTypes { get; set; }
}

public class AuthenticationSchemesEdit_ViewModel : FormSubmit_ViewModel
{
    public string Id { get; set; }
    
    [Display(Name = "Label")]
    public string Label { get; set; }

    [Display(Name = "Developer name")]
    public string DeveloperName { get; set; }

    [Display(Name = "Authentication scheme type")]
    public string AuthenticationSchemeType { get; set; }

    [Display(Name = "Is enabled for public users")]
    public bool IsEnabledForUsers { get; set; }

    [Display(Name = "Is enabled for admins")]
    public bool IsEnabledForAdmins { get; set; }

    [Display(Name = "SAML certificate")]
    public string SamlCertificate { get; set; }

    [Display(Name = "Secret key")]
    public string JwtSecretKey { get; set; }

    [Display(Name = "Sign-in url")]
    public string SignInUrl { get; set; }

    [Display(Name = "Login button text")]
    public string LoginButtonText { get; set; }

    [Display(Name = "Sign-out url")]
    public string SignOutUrl { get; set; }

    [Display(Name = "Magic link expires after this amount of seconds")]
    public int MagicLinkExpiresInSeconds { get; init; } = 900;

    [Display(Name = "Use added security (implement 'jti' claim)")]
    public bool JwtUseHighSecurity { get; init; } = false;
    
    [Display(Name = "IdP entity id")]
    public string SamlIdpEntityId { get; init; }

    public bool IsBuiltInAuth { get; set; }

    //helpers
    public SelectList SupportedAuthenticationSchemeTypes { get; set; }
}

public class AuthenticationSchemesActionsMenu_ViewModel
{
    public string Id { get; set; }
    public bool IsBuiltInAuth { get; set; }
}