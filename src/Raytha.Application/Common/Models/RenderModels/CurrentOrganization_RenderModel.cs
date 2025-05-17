using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Common.Models.RenderModels;

public record CurrentOrganization_RenderModel : IInsertTemplateVariable
{
    public bool EmailAndPasswordIsEnabledForAdmin { get; init; }
    public bool EmailAndPasswordIsEnabledForUsers { get; init; }
    public string OrganizationName { get; init; }
    public string WebsiteUrl { get; init; }
    public string TimeZone { get; init; }
    public string SmtpDefaultFromAddress { get; init; }
    public string SmtpDefaultFromName { get; init; }
    public string DateFormat { get; init; }
    public string HomePageId { get; init; }
    public IEnumerable<AuthenticationScheme_RenderModel> AuthenticationSchemes { get; init; }

    public static CurrentOrganization_RenderModel GetProjection(ICurrentOrganization entity)
    {
        if (entity == null)
            return null;

        return new CurrentOrganization_RenderModel
        {
            EmailAndPasswordIsEnabledForAdmin = entity.AuthenticationSchemes.Any(p =>
                p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName
                && p.IsEnabledForAdmins
            ),
            EmailAndPasswordIsEnabledForUsers = entity.AuthenticationSchemes.Any(p =>
                p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName
                && p.IsEnabledForUsers
            ),
            OrganizationName = entity.OrganizationName,
            WebsiteUrl = entity.WebsiteUrl,
            TimeZone = entity.TimeZone,
            DateFormat = entity.DateFormat,
            SmtpDefaultFromAddress = entity.SmtpDefaultFromAddress,
            SmtpDefaultFromName = entity.SmtpDefaultFromName,
            HomePageId = entity.HomePageId,
            AuthenticationSchemes = entity.AuthenticationSchemes.Select(p =>
                AuthenticationScheme_RenderModel.GetProjection(p)
            ),
        };
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(EmailAndPasswordIsEnabledForAdmin);
        yield return nameof(EmailAndPasswordIsEnabledForUsers);
        yield return nameof(OrganizationName);
        yield return nameof(WebsiteUrl);
        yield return nameof(TimeZone);
        yield return nameof(SmtpDefaultFromAddress);
        yield return nameof(SmtpDefaultFromName);
        yield return nameof(DateFormat);
        yield return nameof(HomePageId);
        foreach (
            var item in AuthenticationScheme_RenderModel
                .FromPrefix("AuthenticationSchemes[0]")
                .GetDeveloperNames()
        )
        {
            yield return item;
        }
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(
                developerName,
                $"CurrentOrganization.{developerName}"
            );
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
