using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.AuthenticationSchemes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public SelectList SupportedAuthenticationSchemeTypes { get; set; }

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Authentication Schemes",
                RouteName = RouteNames.AuthenticationSchemes.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create",
                RouteName = RouteNames.AuthenticationSchemes.Create,
                IsActive = true,
            }
        );

        var supportedAuthenticationTypes = new OrderedDictionary()
        {
            { "", "-- SELECT --" },
            { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
            { AuthenticationSchemeType.Saml.DeveloperName, AuthenticationSchemeType.Saml.Label },
        };
        Form = new FormModel();
        SupportedAuthenticationSchemeTypes = new SelectList(
            supportedAuthenticationTypes,
            "Key",
            "Value"
        );
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateAuthenticationScheme.Command
        {
            Label = Form.Label,
            AuthenticationSchemeType = Form.AuthenticationSchemeType,
            SignInUrl = Form.SignInUrl,
            SignOutUrl = Form.SignOutUrl,
            SamlCertificate = Form.SamlCertificate,
            JwtSecretKey = Form.JwtSecretKey,
            LoginButtonText = Form.LoginButtonText,
            IsEnabledForAdmins = Form.IsEnabledForAdmins,
            IsEnabledForUsers = Form.IsEnabledForUsers,
            DeveloperName = Form.DeveloperName,
            SamlIdpEntityId = Form.SamlIdpEntityId,
            JwtUseHighSecurity = Form.JwtUseHighSecurity,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Authentication scheme was created successfully.");
            return RedirectToPage(RouteNames.AuthenticationSchemes.Edit, new { id = response.Result });
        }
        else
        {
            var supportedAuthenticationTypes = new OrderedDictionary()
            {
                { "", "-- SELECT --" },
                { AuthenticationSchemeType.Jwt.DeveloperName, AuthenticationSchemeType.Jwt.Label },
                {
                    AuthenticationSchemeType.Saml.DeveloperName,
                    AuthenticationSchemeType.Saml.Label
                },
            };
            SupportedAuthenticationSchemeTypes = new SelectList(
                supportedAuthenticationTypes,
                "Key",
                "Value"
            );
            SetErrorMessage(
                "There was an error attempting to create this authentication scheme. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
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
    }
}
