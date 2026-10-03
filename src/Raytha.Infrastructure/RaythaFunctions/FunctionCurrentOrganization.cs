using CSharpVitamins;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;

namespace Raytha.Infrastructure.RaythaFunctions;

/// <summary>
/// The <c>CurrentOrganization</c> a function can see. It is a copy of the organization with the
/// JWT signing secret and the SAML certificate left out, so returning it cannot publish either.
/// </summary>
public sealed class FunctionCurrentOrganization
{
    private FunctionCurrentOrganization(ICurrentOrganization source)
    {
        InitialSetupComplete = source.InitialSetupComplete;
        OrganizationName = source.OrganizationName;
        WebsiteUrl = source.WebsiteUrl;
        TimeZone = source.TimeZone;
        DateFormat = source.DateFormat;
        SmtpDefaultFromAddress = source.SmtpDefaultFromAddress;
        SmtpDefaultFromName = source.SmtpDefaultFromName;
        EmailAndPasswordIsEnabledForAdmins = source.EmailAndPasswordIsEnabledForAdmins;
        EmailAndPasswordIsEnabledForUsers = source.EmailAndPasswordIsEnabledForUsers;
        HomePageId = source.HomePageId;
        HomePageType = source.HomePageType;
        ActiveThemeId = source.ActiveThemeId;
        PathBase = source.PathBase;
        RedirectWebsite = source.RedirectWebsite;
        TimeZoneConverter = source.TimeZoneConverter;
        AuthenticationSchemes = (source.AuthenticationSchemes ?? [])
            .Select(FunctionAuthenticationScheme.From)
            .ToArray();
        ContentTypes = (source.ContentTypes ?? []).ToArray();
    }

    public static FunctionCurrentOrganization From(ICurrentOrganization source) => new(source);

    public bool InitialSetupComplete { get; }
    public string OrganizationName { get; }
    public string WebsiteUrl { get; }
    public string TimeZone { get; }
    public string DateFormat { get; }
    public string SmtpDefaultFromAddress { get; }
    public string SmtpDefaultFromName { get; }
    public bool EmailAndPasswordIsEnabledForAdmins { get; }
    public bool EmailAndPasswordIsEnabledForUsers { get; }
    public ShortGuid? HomePageId { get; }
    public string HomePageType { get; }
    public ShortGuid ActiveThemeId { get; }
    public string PathBase { get; }
    public string RedirectWebsite { get; }
    public OrganizationTimeZoneConverter TimeZoneConverter { get; }
    public IReadOnlyList<FunctionAuthenticationScheme> AuthenticationSchemes { get; }
    public IReadOnlyList<ContentTypeDto> ContentTypes { get; }
}

/// <summary>A scheme as a function sees it: the fields a template would use, and neither secret.</summary>
public sealed class FunctionAuthenticationScheme
{
    public ShortGuid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string AuthenticationSchemeType { get; init; } = string.Empty;
    public bool IsBuiltInAuth { get; init; }
    public bool IsEnabledForUsers { get; init; }
    public bool IsEnabledForAdmins { get; init; }
    public string SignInUrl { get; init; } = string.Empty;
    public string LoginButtonText { get; init; } = string.Empty;
    public string SignOutUrl { get; init; } = string.Empty;
    public int MagicLinkExpiresInSeconds { get; init; }
    public bool JwtUseHighSecurity { get; init; }
    public string SamlIdpEntityId { get; init; } = string.Empty;

    public static FunctionAuthenticationScheme From(AuthenticationSchemeDto scheme) =>
        new()
        {
            Id = scheme.Id,
            Label = scheme.Label,
            DeveloperName = scheme.DeveloperName,
            AuthenticationSchemeType = scheme.AuthenticationSchemeType?.DeveloperName ?? string.Empty,
            IsBuiltInAuth = scheme.IsBuiltInAuth,
            IsEnabledForUsers = scheme.IsEnabledForUsers,
            IsEnabledForAdmins = scheme.IsEnabledForAdmins,
            SignInUrl = scheme.SignInUrl,
            LoginButtonText = scheme.LoginButtonText,
            SignOutUrl = scheme.SignOutUrl,
            MagicLinkExpiresInSeconds = scheme.MagicLinkExpiresInSeconds,
            JwtUseHighSecurity = scheme.JwtUseHighSecurity,
            SamlIdpEntityId = scheme.SamlIdpEntityId,
        };
}
