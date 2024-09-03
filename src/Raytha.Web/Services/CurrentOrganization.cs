using CSharpVitamins;
using MediatR;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.OrganizationSettings;
using Raytha.Application.OrganizationSettings.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace Raytha.Web.Services;

public class CurrentOrganization : ICurrentOrganization
{
    private OrganizationSettingsDto _organizationSettings;
    private IEnumerable<AuthenticationSchemeDto> _authenticationSchemes;
    private IEnumerable<ContentTypeDto> _contentTypes;

    private readonly ISender Mediator;
    private readonly ICurrentOrganizationConfiguration Configuration;

    public CurrentOrganization(ISender mediator, ICurrentOrganizationConfiguration configuration)
    {
        Mediator = mediator;
        Configuration = configuration;
    }

    private OrganizationSettingsDto OrganizationSettings
    {
        get
        {
            if (_organizationSettings == null)
            {
                var response = Mediator.Send(new GetOrganizationSettings.Query()).GetAwaiter().GetResult();
                _organizationSettings = response.Result;
            }
            return _organizationSettings;
        }
    }

    public IEnumerable<AuthenticationSchemeDto> AuthenticationSchemes 
    { 
        get
        {
            if (_authenticationSchemes == null)
            {
                var response = Mediator.Send(new GetAuthenticationSchemes.Query()).GetAwaiter().GetResult();
                _authenticationSchemes = response.Result.Items;
            }
            return _authenticationSchemes;
        }
    }

    public IEnumerable<ContentTypeDto> ContentTypes
    {
        get
        {
            if (_contentTypes == null)
            {
                var response = Mediator.Send(new GetContentTypes.Query
                {
                    OrderBy = $"{BuiltInContentTypeField.CreationTime.DeveloperName} {SortOrder.ASCENDING}",
                }).GetAwaiter().GetResult();
                _contentTypes = response.Result.Items;
            }
            return _contentTypes;
        }
    }

    public bool EmailAndPasswordIsEnabledForAdmins => AuthenticationSchemes.Any(p => p.IsEnabledForAdmins && p.AuthenticationSchemeType.DeveloperName == AuthenticationSchemeType.EmailAndPassword);
    public bool EmailAndPasswordIsEnabledForUsers => AuthenticationSchemes.Any(p => p.IsEnabledForUsers && p.AuthenticationSchemeType.DeveloperName == AuthenticationSchemeType.EmailAndPassword);
    
    public bool InitialSetupComplete => OrganizationSettings != null;

    public string OrganizationName => OrganizationSettings.OrganizationName;

    public string WebsiteUrl => OrganizationSettings.WebsiteUrl;

    public string TimeZone => OrganizationSettings.TimeZone;

    public string SmtpDefaultFromAddress => OrganizationSettings.SmtpDefaultFromAddress;

    public string SmtpDefaultFromName => OrganizationSettings.SmtpDefaultFromName;

    public string DateFormat => OrganizationSettings.DateFormat;

    public ShortGuid? HomePageId => OrganizationSettings.HomePageId;

    public string HomePageType => OrganizationSettings.HomePageType;
    public ShortGuid ActiveThemeId => OrganizationSettings.ActiveThemeId;

    public OrganizationTimeZoneConverter TimeZoneConverter => OrganizationTimeZoneConverter.From(TimeZone, DateFormat);

    public string PathBase => Configuration.PathBase;
    public string RedirectWebsite => Configuration.RedirectWebsite;
}
