using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.AuthenticationSchemes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public bool IsBuiltInAuth { get; set; }
    public SelectList SupportedAuthenticationSchemeTypes { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var supportedAuthenticationTypes = new OrderedDictionary()
        {
            {
                AuthenticationSchemeType.EmailAndPassword.DeveloperName,
                AuthenticationSchemeType.EmailAndPassword.Label
            },
            {
                AuthenticationSchemeType.MagicLink.DeveloperName,
                AuthenticationSchemeType.MagicLink.Label
            },
            { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
            { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label },
        };
        var response = await Mediator.Send(new GetAuthenticationSchemeById.Query { Id = id });

        Form = new FormModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            IsEnabledForAdmins = response.Result.IsEnabledForAdmins,
            IsEnabledForUsers = response.Result.IsEnabledForUsers,
            JwtSecretKey = response.Result.JwtSecretKey,
            SamlCertificate = response.Result.SamlCertificate,
            AuthenticationSchemeType = response.Result.AuthenticationSchemeType,
            SignInUrl = response.Result.SignInUrl,
            SignOutUrl = response.Result.SignOutUrl,
            LoginButtonText = response.Result.LoginButtonText,
            SamlIdpEntityId = response.Result.SamlIdpEntityId,
            JwtUseHighSecurity = response.Result.JwtUseHighSecurity,
            MagicLinkExpiresInSeconds = response.Result.MagicLinkExpiresInSeconds,
        };
        IsBuiltInAuth = response.Result.IsBuiltInAuth;
        SupportedAuthenticationSchemeTypes = new SelectList(
            supportedAuthenticationTypes,
            "Key",
            "Value"
        );
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditAuthenticationScheme.Command
        {
            Id = id,
            Label = Form.Label,
            AuthenticationSchemeType = Form.AuthenticationSchemeType,
            SignInUrl = Form.SignInUrl,
            SignOutUrl = Form.SignOutUrl,
            SamlCertificate = Form.SamlCertificate,
            JwtSecretKey = Form.JwtSecretKey,
            LoginButtonText = Form.LoginButtonText,
            IsEnabledForAdmins = Form.IsEnabledForAdmins,
            IsEnabledForUsers = Form.IsEnabledForUsers,
            SamlIdpEntityId = Form.SamlIdpEntityId,
            JwtUseHighSecurity = Form.JwtUseHighSecurity,
            MagicLinkExpiresInSeconds = Form.MagicLinkExpiresInSeconds,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Authentication scheme was updated successfully.");
            return RedirectToPage("/AuthenticationSchemes/Edit", new { id });
        }
        else
        {
            var supportedAuthenticationTypes = new OrderedDictionary()
            {
                {
                    AuthenticationSchemeType.EmailAndPassword.DeveloperName,
                    AuthenticationSchemeType.EmailAndPassword.Label
                },
                {
                    AuthenticationSchemeType.MagicLink.DeveloperName,
                    AuthenticationSchemeType.MagicLink.Label
                },
                { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
                {
                    AuthenticationSchemeType.Saml.DeveloperName,
                    AuthenticationSchemeType.Saml.Label
                },
            };
            var currentScheme = await Mediator.Send(
                new GetAuthenticationSchemeById.Query { Id = id }
            );
            SetErrorMessage(
                "There was an error attempting to update this authentication scheme. See the error below.",
                response.GetErrors()
            );
            IsBuiltInAuth = currentScheme.Result.IsBuiltInAuth;
            SupportedAuthenticationSchemeTypes = new SelectList(
                supportedAuthenticationTypes,
                "Key",
                "Value"
            );
            return Page();
        }
    }

    public record FormModel
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
    }
}
