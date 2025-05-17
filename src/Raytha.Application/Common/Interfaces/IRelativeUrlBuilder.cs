namespace Raytha.Application.Common.Interfaces;

public interface IRelativeUrlBuilder
{
    string AdminLoginUrl(string returnUrl = "");
    string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "");
    string AdminForgotPasswordCompleteUrl(string token);
    string MediaRedirectToFileUrl(string objectKey);
    string MediaFileLocalStorageUrl(string objectKey);
    string UserLoginUrl(string returnUrl = "");
    string UserLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "");
    string UserForgotPasswordCompleteUrl(string token);
    string GetBaseUrl();
    string GetSingleSignOnCallbackJwtUrl(
        string area,
        string developerName,
        string signinUrl,
        string returnUrl = ""
    );
    string GetSingleSignOnCallbackSamlUrl(
        string area,
        string developerName,
        string samlIdpEntityId,
        string signinUrl,
        string returnUrl = ""
    );
}
