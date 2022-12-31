using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public record CurrentUser_RenderModel : IInsertTemplateVariable
{
    public string UserId { get; init; }
    public bool IsAuthenticated { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public string SsoId { get; init; }
    public string AuthenticationScheme { get; init; }
    public string RemoteIpAddress { get; init; }
    public string EmailAddress { get; init; }
    public bool IsAdmin { get; init; }
    public string[] Roles { get; init; }
    public string[] UserGroups { get; init; }

    public static CurrentUser_RenderModel GetProjection(ICurrentUser entity)
    {
        if (entity == null)
            return null;

        return new CurrentUser_RenderModel
        {
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            AuthenticationScheme = entity.AuthenticationScheme,
            LastModificationTime = entity.LastModificationTime,
            SsoId = entity.SsoId,
            IsAuthenticated = entity.IsAuthenticated,
            RemoteIpAddress = entity.RemoteIpAddress,
            EmailAddress = entity.EmailAddress,
            UserId = entity.UserId,
            IsAdmin = entity.IsAdmin,
            Roles = entity.Roles,
            UserGroups = entity.UserGroups
        };
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(IsAuthenticated);
        yield return nameof(LastModificationTime);
        yield return nameof(UserId);
        yield return nameof(FirstName);
        yield return nameof(LastName);
        yield return nameof(FullName);
        yield return nameof(EmailAddress);
        yield return nameof(SsoId);
        yield return nameof(AuthenticationScheme);
        yield return nameof(RemoteIpAddress);
        yield return nameof(IsAdmin);
        yield return nameof(Roles);
        yield return nameof(UserGroups);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"CurrentUser.{developerName}");
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}
