using System.Web;

namespace Raytha.Application.Common.Interfaces;

public interface IRelativeUrlBuilder
{
    string AdminLoginUrl(string returnUrl = "");
    string AdminForgotPasswordCompleteUrl(string token);
    string MediaRedirectToFileUrl(string objectKey);

    /// <summary>
    /// Root-relative form of <see cref="MediaRedirectToFileUrl"/>. Use it wherever the URL can be
    /// saved into content, so the content keeps working when the site moves to another host.
    /// </summary>
    string MediaRedirectToFilePath(string objectKey);
    string MediaFileLocalStorageUrl(string objectKey);
    string MediaCloudUploadPresignUrl();
    string MediaCloudUploadCreateAfterUploadUrl();
    string MediaDirectUploadUrl();
    string UserLoginUrl(string returnUrl = "");
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
