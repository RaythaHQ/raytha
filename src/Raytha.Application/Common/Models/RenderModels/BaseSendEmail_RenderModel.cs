using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Models.RenderModels;

public abstract record BaseSendToUserEmail_RenderModel : IInsertTemplateVariable
{
    public string Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string FullName
    {
        get { return $"{FirstName} {LastName}"; }
    }
    public string SsoId { get; init; } = string.Empty;
    public string AuthenticationScheme { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string LoginUrl { get; init; } = string.Empty;
    public dynamic CustomAttributes { get; init; }

    public virtual IEnumerable<string> GetDeveloperNames()
    {
        yield return nameof(Id);
        yield return nameof(FirstName);
        yield return nameof(LastName);
        yield return nameof(EmailAddress);
        yield return nameof(FullName);
        yield return nameof(SsoId);
        yield return nameof(AuthenticationScheme);
        yield return nameof(IsAdmin);
        yield return nameof(LoginUrl);
        yield return nameof(CustomAttributes);
    }

    public virtual IEnumerable<KeyValuePair<string, string>> GetTemplateVariables()
    {
        foreach (var developerName in GetDeveloperNames())
        {
            yield return new KeyValuePair<string, string>(developerName, $"Target.{developerName}");
        }
    }

    public virtual string GetTemplateVariablesAsForEachLiquidSyntax()
    {
        return string.Empty;
    }
}

public abstract record BaseSendWelcomeEmail_RenderModel : BaseSendToUserEmail_RenderModel
{
    public bool LoginWithEmailAndPasswordIsEnabled { get; init; }
    public string? NewPassword { get; init; }

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(LoginWithEmailAndPasswordIsEnabled);
        yield return nameof(NewPassword);
    }
}

public abstract record BaseSendPasswordChanged_RenderModel : BaseSendToUserEmail_RenderModel { }

public abstract record BaseSendPasswordReset_RenderModel : BaseSendToUserEmail_RenderModel
{
    public string? NewPassword { get; init; }

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(NewPassword);
    }
}
