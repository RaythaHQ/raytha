using CSharpVitamins;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;

namespace Raytha.Application.Common.Interfaces;

public interface ICurrentOrganization
{
    bool InitialSetupComplete { get; }
    string OrganizationName { get; }
    string WebsiteUrl { get; }
    string TimeZone { get; }
    string DateFormat { get; }
    string SmtpDefaultFromAddress { get; }
    string SmtpDefaultFromName { get; }
    bool EmailAndPasswordIsEnabledForAdmins { get; }
    bool EmailAndPasswordIsEnabledForUsers { get; }
    ShortGuid? HomePageId { get; }
    string HomePageType { get; }
    ShortGuid ActiveThemeId { get; }

    string PathBase { get; }
    string RedirectWebsite { get; }

    OrganizationTimeZoneConverter TimeZoneConverter { get; }
    IEnumerable<AuthenticationSchemeDto> AuthenticationSchemes { get; }
    IEnumerable<ContentTypeDto> ContentTypes { get; }
}