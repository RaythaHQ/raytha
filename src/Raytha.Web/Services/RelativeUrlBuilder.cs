using Microsoft.AspNetCore.WebUtilities;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;

namespace RaythaZero.Web.Services;

public class RelativeUrlBuilder : IRelativeUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _generator;
    private readonly ICurrentOrganization _currentOrganization;

    public RelativeUrlBuilder(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator generator,
        ICurrentOrganization currentOrganization
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _generator = generator;
        _currentOrganization = currentOrganization;
    }

    public string AdminLoginUrl(string returnUrl = "") =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/LoginWithEmailAndPassword",
            new { area = "Admin", returnUrl }
        );

    public string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/LoginWithMagicLinkComplete",
            new
            {
                area = "Admin",
                token,
                returnUrl,
            }
        );

    public string AdminForgotPasswordCompleteUrl(string token) =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/ForgotPasswordComplete",
            new { area = "Admin", token }
        );

    public string MediaRedirectToFileUrl(string objectKey) =>
        ResolveUrlIfHttpContextAccessExists(
            "RedirectToFileUrlByObjectKey",
            new
            {
                area = "Admin",
                controller = "MediaItems",
                action = "RedirectToFileUrlByObjectKey",
                objectKey,
            }
        );

    public string MediaFileLocalStorageUrl(string objectKey) =>
        GetBaseUrl() + $"/_static-files/{objectKey}";

    public string UserLoginUrl(string returnUrl = "") =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/LoginWithEmailAndPassword",
            new { area = "Public", returnUrl }
        );

    public string UserLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/LoginWithMagicLinkComplete",
            new
            {
                area = "Public",
                token,
                returnUrl,
            }
        );

    public string UserForgotPasswordCompleteUrl(string token) =>
        ResolveUrlIfHttpContextAccessExists(
            "/Login/ForgotPasswordComplete",
            new { area = "Public", token }
        );

    public string GetBaseUrl() =>
        _httpContextAccessor.HttpContext == null
            ? $"{_currentOrganization.PathBase}"
            : $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_currentOrganization.PathBase}";

    private string ResolveUrlIfHttpContextAccessExists(string page, object values)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return _generator.GetPathByPage(page, values: values);
        }
        else
        {
            return _generator.GetUriByPage(_httpContextAccessor.HttpContext, page, values: values);
        }
    }

    public string GetSingleSignOnCallbackJwtUrl(
        string area,
        string developerName,
        string signinUrl,
        string returnUrl = ""
    )
    {
        var routeValues = new { area, developerName };

        string callbackUrl = _generator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            "AdminLoginWithJwt",
            routeValues
        );
        if (!string.IsNullOrEmpty(returnUrl))
        {
            var parametersToAdd = new Dictionary<string, string> { { "returnUrl", returnUrl } };
            callbackUrl = QueryHelpers.AddQueryString(callbackUrl, parametersToAdd);
        }
        var setCallbackParams = new Dictionary<string, string>
        {
            { "raytha_callback_url", callbackUrl },
        };
        var loginUrl = QueryHelpers.AddQueryString(signinUrl, setCallbackParams);
        return loginUrl;
    }

    public string GetSingleSignOnCallbackSamlUrl(
        string area,
        string developerName,
        string samlIdpEntityId,
        string signinUrl,
        string returnUrl = ""
    )
    {
        var routeValues = new { area, developerName };

        var acsUrl = _generator.GetUriByRouteValues(
            _httpContextAccessor.HttpContext,
            "AdminLoginWithSaml",
            routeValues
        );
        var samlRequest = SamlUtility.GetSamlRequestAsBase64(acsUrl, samlIdpEntityId);
        var parametersToAdd = new Dictionary<string, string> { { "SAMLRequest", samlRequest } };

        if (!string.IsNullOrEmpty(returnUrl))
            parametersToAdd.Add("RelayState", returnUrl);

        var loginUrl = QueryHelpers.AddQueryString(signinUrl, parametersToAdd);
        return loginUrl;
    }
}
