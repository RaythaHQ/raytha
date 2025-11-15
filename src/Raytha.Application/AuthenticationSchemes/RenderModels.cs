using System.Linq.Expressions;
using System.Text;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models.RenderModels;

namespace Raytha.Application.AuthenticationSchemes;

public record AuthenticationScheme_RenderModel : IInsertTemplateVariable
{
    private string _prefix = string.Empty;

    private AuthenticationScheme_RenderModel() { }

    private AuthenticationScheme_RenderModel(string prefix)
    {
        _prefix = prefix;
    }

    public string Id { get; init; }
    public string Label { get; init; }
    public string DeveloperName { get; set; }
    public bool IsBuiltInAuth { get; init; }
    public bool IsEnabledForUsers { get; init; }
    public bool IsEnabledForAdmins { get; init; }
    public string SignInUrl { get; init; } = string.Empty;
    public string LoginButtonText { get; init; } = string.Empty;
    public string SignOutUrl { get; init; } = string.Empty;

    public static Expression<
        Func<AuthenticationSchemeDto, AuthenticationScheme_RenderModel>
    > GetProjection()
    {
        return authScheme => GetProjection(authScheme);
    }

    public static AuthenticationScheme_RenderModel GetProjection(AuthenticationSchemeDto entity)
    {
        if (entity == null)
            return null;

        return new AuthenticationScheme_RenderModel
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            SignInUrl = entity.SignInUrl,
            SignOutUrl = entity.SignOutUrl,
            LoginButtonText = entity.LoginButtonText,
            IsBuiltInAuth = entity.IsBuiltInAuth,
            IsEnabledForUsers = entity.IsEnabledForUsers,
            IsEnabledForAdmins = entity.IsEnabledForAdmins,
        };
    }

    private IEnumerable<string> GetNakedDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(Label);
        yield return nameof(DeveloperName);
        yield return nameof(SignInUrl);
        yield return nameof(SignOutUrl);
        yield return nameof(LoginButtonText);
        yield return nameof(IsBuiltInAuth);
        yield return nameof(IsEnabledForUsers);
        yield return nameof(IsEnabledForAdmins);
    }

    public IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in GetNakedDeveloperNames())
        {
            if (!string.IsNullOrEmpty(_prefix))
            {
                yield return $"{_prefix}.{developerName}";
            }
            else
            {
                yield return developerName;
            }
        }
    }

    public static AuthenticationScheme_RenderModel FromPrefix(string prefix)
    {
        return new AuthenticationScheme_RenderModel(prefix);
    }

    public IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"{developerName}");
        }
    }

    public string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        StringBuilder sb = new StringBuilder(string.Empty);
        sb.AppendLine($"{{% for item in CurrentOrganization.AuthenticationSchemes %}}");
        foreach (var developerName in GetDeveloperNames())
        {
            sb.AppendLine($"{developerName}: {{ item.{developerName} }}");
        }
        sb.AppendLine($"{{% endfor %}}");
        return sb.ToString();
    }
}
