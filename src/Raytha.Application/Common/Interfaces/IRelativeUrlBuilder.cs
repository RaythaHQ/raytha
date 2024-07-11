namespace Raytha.Application.Common.Interfaces;

public interface IRelativeUrlBuilder
{
    string AdminLoginUrl();
    string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "");
    string AdminForgotPasswordCompleteUrl(string token);
    string MediaRedirectToFileUrl(string objectKey);
    string MediaFileLocalStorageUrl(string objectKey);
    string UserLoginUrl();
    string UserLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "");
    string UserForgotPasswordCompleteUrl(string token);
    string GetBaseUrl();
}
