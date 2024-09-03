using CSharpVitamins;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentItems;
using System.Linq.Expressions;

namespace Raytha.Application.OrganizationSettings;

public record OrganizationSettingsDto
{
    public string OrganizationName { get; init; } = string.Empty;
    public string WebsiteUrl { get; init; } = string.Empty;
    public string TimeZone { get; init; } = DateTimeExtensions.DEFAULT_TIMEZONE;
    public string DateFormat { get; init; } = DateTimeExtensions.DEFAULT_DATE_FORMAT;
    public string SmtpDefaultFromAddress { get; init; } = string.Empty;
    public string SmtpDefaultFromName { get; init; } = string.Empty;
    public bool SmtpOverrideSystem { get; init; } = false;
    public string SmtpHost { get; init; } = string.Empty;
    public int? SmtpPort { get; init; }
    public string SmtpUsername { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public ShortGuid? HomePageId { get; init; }
    public string HomePageType { get; init; } = string.Empty;
    public ShortGuid ActiveThemeId { get; init; }

    public static Expression<Func<Domain.Entities.OrganizationSettings, OrganizationSettingsDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static OrganizationSettingsDto GetProjection(Domain.Entities.OrganizationSettings entity)
    {
        if (entity == null)
            return null;

        return new OrganizationSettingsDto
        {
            OrganizationName = entity.OrganizationName,
            WebsiteUrl = entity.WebsiteUrl,
            TimeZone = entity.TimeZone,
            DateFormat = entity.DateFormat,
            SmtpDefaultFromAddress = entity.SmtpDefaultFromAddress,
            SmtpDefaultFromName = entity.SmtpDefaultFromName,
            SmtpOverrideSystem = entity.SmtpOverrideSystem,
            SmtpHost = entity.SmtpHost,
            SmtpPort = entity.SmtpPort,
            SmtpPassword = entity.SmtpPassword,
            SmtpUsername = entity.SmtpUsername,
            HomePageId = entity.HomePageId,
            HomePageType = entity.HomePageType,
            ActiveThemeId = entity.ActiveThemeId,
        };
    }
}
