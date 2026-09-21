using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Login;
using Raytha.Application.Login.Commands;
using Raytha.Application.Login.Queries;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Domain.ValueObjects;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Session endpoints for the admin SPA under <c>/raytha/api/auth</c>. Sign-in issues the same
/// cookie principal as <c>BaseAdminLoginPageModel.LoginWithClaims</c>; the full claim set is
/// hydrated on the next request by <c>CustomCookieAuthenticationEvents</c>.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/raytha/api/auth").WithTags("Admin auth");

        group.MapGet("/me", Me).RequireAuthorization(RaythaClaimTypes.IsAdmin);
        group.MapPost("/login", Login).AllowAnonymous();
        group.MapPost("/logout", Logout).AllowAnonymous();
        group.MapGet("/schemes", Schemes).AllowAnonymous();

        group.MapPost("/magic-link", BeginMagicLink).AllowAnonymous();
        group.MapPost("/magic-link/complete", CompleteMagicLink).AllowAnonymous();

        group.MapPost("/forgot-password", BeginForgotPassword).AllowAnonymous();
        group.MapGet("/forgot-password/validate", ValidateForgotPasswordToken).AllowAnonymous();
        group.MapPost("/forgot-password/complete", CompleteForgotPassword).AllowAnonymous();

        group.MapGet("/setup", SetupStatus).AllowAnonymous();
        group.MapGet("/setup/status", SetupStatus).AllowAnonymous();
        group.MapPost("/setup", Setup).AllowAnonymous();

        return endpoints;
    }

    private static IResult Me(
        HttpContext http,
        [FromServices] ICurrentUser currentUser,
        [FromServices] ICurrentOrganization organization,
        [FromServices] IFileStorageProviderSettings fileStorage
    )
    {
        var claims = http.User.Claims;
        string[] Values(string type) => claims.Where(c => c.Type == type).Select(c => c.Value).Distinct().ToArray();

        return Results.Ok(
            new
            {
                id = currentUser.UserId?.ToString() ?? string.Empty,
                email = currentUser.EmailAddress,
                firstName = currentUser.FirstName,
                lastName = currentUser.LastName,
                fullName = currentUser.FullName,
                isAdmin = currentUser.IsAdmin,
                authenticationScheme = currentUser.AuthenticationScheme,
                roles = Values(ClaimTypes.Role),
                permissions = Values(RaythaClaimTypes.SystemPermissions),
                contentTypePermissions = Values(RaythaClaimTypes.ContentTypePermissions),
                userGroups = Values(RaythaClaimTypes.UserGroups),
                initialSetupComplete = organization.InitialSetupComplete,
                useDirectUploadToCloud = fileStorage.UseDirectUploadToCloud,
                emailAndPasswordEnabled = organization.EmailAndPasswordIsEnabledForAdmins,
                organization = new
                {
                    name = organization.OrganizationName,
                    websiteUrl = organization.WebsiteUrl,
                    timeZone = organization.TimeZone,
                    dateFormat = organization.DateFormat,
                    pathBase = organization.PathBase,
                },
            }
        );
    }

    private static async Task<IResult> Login(
        HttpContext http,
        [FromBody] LoginRequest body,
        [FromServices] ISender mediator
    )
    {
        var response = await mediator.Send(
            new LoginWithEmailAndPassword.Command
            {
                EmailAddress = body.Email ?? string.Empty,
                Password = body.Password ?? string.Empty,
            }
        );

        if (!response.Success)
        {
            return AdminResults.Problem(response.GetErrors(), StatusCodes.Status401Unauthorized);
        }

        if (!response.Result.IsAdmin)
        {
            return AdminResults.Problem(
                "This account does not have administrator access.",
                StatusCodes.Status403Forbidden
            );
        }

        await SignInAsync(http, response.Result, body.RememberMe ?? true);
        return Results.Ok(new { id = response.Result.Id.ToString(), requiresTwoFactor = false });
    }

    private static async Task<IResult> Logout(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> Schemes(
        [FromQuery] string? returnUrl,
        [FromServices] ISender mediator,
        [FromServices] ICurrentOrganization organization
    )
    {
        var response = await mediator.Send(
            new GetAuthenticationSchemes.Query { IsEnabledForAdmins = true, PageSize = int.MaxValue }
        );
        if (!response.Success)
        {
            return AdminResults.Problem(response.GetErrors());
        }

        var schemes = response.Result.Items.Select(scheme =>
        {
            var type = scheme.AuthenticationSchemeType.DeveloperName;
            var isSso = type == AuthenticationSchemeType.Jwt.DeveloperName
                || type == AuthenticationSchemeType.Saml.DeveloperName;
            string? signInUrl = null;
            if (isSso)
            {
                signInUrl = $"{organization.PathBase}/raytha/login/sso/{Uri.EscapeDataString(scheme.DeveloperName)}";
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/'))
                {
                    signInUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
                }
            }

            return new
            {
                label = scheme.Label,
                developerName = scheme.DeveloperName,
                schemeType = type,
                loginButtonText = scheme.LoginButtonText,
                isBuiltInAuth = scheme.IsBuiltInAuth,
                signInUrl,
            };
        });

        return Results.Ok(schemes);
    }

    private static async Task<IResult> BeginMagicLink(
        [FromBody] EmailRequest body,
        [FromServices] ISender mediator
    )
    {
        var response = await mediator.Send(
            new BeginLoginWithMagicLink.Command
            {
                EmailAddress = body.Email ?? string.Empty,
                ReturnUrl = SafeReturnUrl(body.ReturnUrl),
            }
        );
        return AdminResults.NoContent(response);
    }

    private static async Task<IResult> CompleteMagicLink(
        HttpContext http,
        [FromBody] TokenRequest body,
        [FromServices] ISender mediator
    )
    {
        if (string.IsNullOrWhiteSpace(body.Token))
        {
            return AdminResults.Problem("Login token is missing.");
        }

        var response = await mediator.Send(new CompleteLoginWithMagicLink.Command { Id = body.Token });
        if (!response.Success)
        {
            return AdminResults.Problem(response.GetErrors(), StatusCodes.Status401Unauthorized);
        }

        if (!response.Result.IsAdmin)
        {
            return AdminResults.Problem(
                "This account does not have administrator access.",
                StatusCodes.Status403Forbidden
            );
        }

        await SignInAsync(http, response.Result, rememberMe: true);
        return Results.Ok(
            new { id = response.Result.Id.ToString(), returnUrl = SafeReturnUrl(body.ReturnUrl) }
        );
    }

    private static async Task<IResult> BeginForgotPassword(
        [FromBody] EmailRequest body,
        [FromServices] ISender mediator,
        [FromServices] ICurrentOrganization organization
    )
    {
        if (!organization.EmailAndPasswordIsEnabledForAdmins)
        {
            return AdminResults.Problem(
                "Authentication scheme disabled for administrators.",
                StatusCodes.Status403Forbidden
            );
        }

        var response = await mediator.Send(
            new Raytha.Application.Login.Commands.BeginForgotPassword.Command
            {
                EmailAddress = body.Email ?? string.Empty,
            }
        );
        return AdminResults.NoContent(response);
    }

    private static async Task<IResult> ValidateForgotPasswordToken(
        [FromQuery] string? token,
        [FromServices] ISender mediator,
        [FromServices] ICurrentOrganization organization
    )
    {
        if (!organization.EmailAndPasswordIsEnabledForAdmins)
        {
            return AdminResults.Problem(
                "Authentication scheme disabled for administrators.",
                StatusCodes.Status403Forbidden
            );
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AdminResults.Problem("Forgot password token is missing.");
        }

        var response = await mediator.Send(new GetForgotPasswordTokenValidity.Query { Id = token });
        return response.Success
            ? Results.Ok(new { valid = true })
            : AdminResults.Problem(response.GetErrors());
    }

    private static async Task<IResult> CompleteForgotPassword(
        [FromBody] CompleteForgotPasswordRequest body,
        [FromServices] ISender mediator
    )
    {
        if (string.IsNullOrWhiteSpace(body.Token))
        {
            return AdminResults.Problem("Forgot password token is missing.");
        }

        var response = await mediator.Send(
            new Raytha.Application.Login.Commands.CompleteForgotPassword.Command
            {
                Id = body.Token,
                NewPassword = body.NewPassword ?? string.Empty,
                ConfirmNewPassword = body.ConfirmNewPassword ?? body.NewPassword ?? string.Empty,
            }
        );
        return AdminResults.NoContent(response);
    }

    private static IResult SetupStatus(
        [FromServices] ICurrentOrganization organization,
        [FromServices] IEmailerConfiguration emailer
    )
    {
        var required = !organization.InitialSetupComplete;
        return Results.Ok(
            new
            {
                required,
                initialSetupComplete = !required,
                missingSmtpEnvironmentVariables = emailer.IsMissingSmtpEnvVars(),
                timeZones = required ? DateTimeExtensions.GetTimeZoneDisplayNames() : null,
            }
        );
    }

    private static async Task<IResult> Setup(
        HttpContext http,
        [FromBody] SetupRequest body,
        [FromServices] ISender mediator,
        [FromServices] ICurrentOrganization organization
    )
    {
        if (organization.InitialSetupComplete)
        {
            return AdminResults.Problem("Initial setup has already been completed.", StatusCodes.Status409Conflict);
        }

        var email = body.Email ?? string.Empty;
        var firstName = body.FirstName ?? string.Empty;
        var lastName = body.LastName ?? string.Empty;
        var fallbackWebsiteUrl = $"{http.Request.Scheme}://{http.Request.Host}{organization.PathBase}";

        var response = await mediator.Send(
            new InitialSetup.Command
            {
                FirstName = firstName,
                LastName = lastName,
                SuperAdminEmailAddress = email,
                SuperAdminPassword = body.Password ?? string.Empty,
                OrganizationName = body.OrganizationName.IfNullOrEmpty("My Organization"),
                WebsiteUrl = body.WebsiteUrl.IfNullOrEmpty(fallbackWebsiteUrl),
                TimeZone = body.TimeZone.IfNullOrEmpty(DateTimeExtensions.DEFAULT_TIMEZONE),
                SmtpDefaultFromAddress = body.SmtpDefaultFromAddress.IfNullOrEmpty(email),
                SmtpDefaultFromName = body.SmtpDefaultFromName.IfNullOrEmpty(
                    $"{firstName} {lastName}".Trim().IfNullOrEmpty("Raytha")
                ),
                SmtpHost = body.SmtpHost ?? string.Empty,
                SmtpPort = body.SmtpPort,
                SmtpUsername = body.SmtpUsername ?? string.Empty,
                SmtpPassword = body.SmtpPassword ?? string.Empty,
            }
        );

        if (!response.Success)
        {
            return AdminResults.Problem(response.GetErrors());
        }

        // Sign the new super admin in so the SPA can land on the dashboard directly.
        var login = await mediator.Send(
            new LoginWithEmailAndPassword.Command { EmailAddress = email, Password = body.Password ?? string.Empty }
        );
        if (login.Success && login.Result.IsAdmin)
        {
            await SignInAsync(http, login.Result, rememberMe: true);
        }

        return Results.Created((string?)null, new { id = response.Result.ToString() });
    }

    /// <summary>Mirror of <c>BaseAdminLoginPageModel.LoginWithClaims</c>.</summary>
    internal static async Task SignInAsync(HttpContext http, LoginDto user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(RaythaClaimTypes.LastModificationTime, user.LastModificationTime.ToString() ?? string.Empty),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = rememberMe }
        );
    }

    /// <summary>Only local, absolute-path return URLs survive; anything else is dropped.</summary>
    private static string? SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return null;
        }
        return returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : null;
    }

    public sealed record LoginRequest(string? Email, string? Password, bool? RememberMe);

    public sealed record EmailRequest(string? Email, string? ReturnUrl);

    public sealed record TokenRequest(string? Token, string? ReturnUrl);

    public sealed record CompleteForgotPasswordRequest(
        string? Token,
        string? NewPassword,
        string? ConfirmNewPassword
    );

    public sealed record SetupRequest(
        string? Email,
        string? Password,
        string? FirstName,
        string? LastName,
        string? OrganizationName,
        string? WebsiteUrl,
        string? TimeZone,
        string? SmtpDefaultFromAddress,
        string? SmtpDefaultFromName,
        string? SmtpHost,
        int? SmtpPort,
        string? SmtpUsername,
        string? SmtpPassword
    );
}
