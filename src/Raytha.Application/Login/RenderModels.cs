using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login;

public record SendBeginForgotPassword_RenderModel : BaseSendToUserEmail_RenderModel
{
    public string ForgotPasswordCompleteUrl { get; init; } = string.Empty;

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(ForgotPasswordCompleteUrl);
    }
}

public record SendBeginLoginWithMagicLink_RenderModel : BaseSendToUserEmail_RenderModel
{
    public string LoginWithMagicLinkCompleteUrl { get; init; } = string.Empty;
    public int MagicLinkExpiresInSeconds { get; init; }

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(LoginWithMagicLinkCompleteUrl);
        yield return nameof(MagicLinkExpiresInSeconds);
    }
}

public record SendCompletedForgotPassword_RenderModel : BaseSendToUserEmail_RenderModel
{
}

public record LoginSubmit_RenderModel : BaseFormSubmit_RenderModel
{
    public string ReturnUrl { get; init; } = string.Empty;
    public bool ShowOrLoginWithSection => HasLoginByMagicLink || HasLoginBySingleSignOn;
    public bool HasLoginByEmailAndPassword => AuthenticationSchemes.Any(p => p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName);
    public bool HasLoginByMagicLink => AuthenticationSchemes.Any(p => p.DeveloperName == AuthenticationSchemeType.MagicLink.DeveloperName);
    public bool HasLoginBySingleSignOn => AuthenticationSchemes.Any(p => !p.IsBuiltInAuth);

    public AuthenticationScheme_RenderModel EmailAndPassword => HasLoginByEmailAndPassword ? AuthenticationSchemes.First(p => p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName) : null;
    public AuthenticationScheme_RenderModel MagicLink => HasLoginByMagicLink ? AuthenticationSchemes.First(p => p.DeveloperName == AuthenticationSchemeType.MagicLink.DeveloperName) : null;
    public IEnumerable<AuthenticationScheme_RenderModel> SingleSignOns => HasLoginBySingleSignOn ? AuthenticationSchemes.Where(p => !p.IsBuiltInAuth) : null;
    public IEnumerable<AuthenticationScheme_RenderModel> AuthenticationSchemes { get; init; } = new List<AuthenticationScheme_RenderModel>();

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(ReturnUrl);
        yield return nameof(ShowOrLoginWithSection);
        yield return nameof(HasLoginByEmailAndPassword);
        yield return nameof(HasLoginByMagicLink);
        yield return nameof(HasLoginBySingleSignOn);
        foreach (var item in AuthenticationScheme_RenderModel.FromPrefix("EmailAndPassword").GetDeveloperNames())
        {
            yield return item;
        }
        foreach (var item in AuthenticationScheme_RenderModel.FromPrefix("MagicLink").GetDeveloperNames())
        {
            yield return item;
        }
        foreach (var item in AuthenticationScheme_RenderModel.FromPrefix("SingleSignOns[0]").GetDeveloperNames())
        {
            yield return item;
        }
    }
}

public record ForgotPasswordSubmit_RenderModel : BaseFormSubmit_RenderModel
{

}

public record ForgotPasswordCompleteSubmit_RenderModel : BaseFormSubmit_RenderModel
{
    public string Token { get; init; } = string.Empty;

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(Token);
    }
}

public record CreateUserSubmit_RenderModel : BaseFormSubmit_RenderModel
{
    public string EmailAddress { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    public override IEnumerable<string> GetDeveloperNames()
    {
        foreach (var developerName in base.GetDeveloperNames())
        {
            yield return developerName;
        }
        yield return nameof(EmailAddress);
        yield return nameof(FirstName);
        yield return nameof(LastName);
    }
}