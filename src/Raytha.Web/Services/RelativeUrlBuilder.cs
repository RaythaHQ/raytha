using Microsoft.AspNetCore.WebUtilities;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Services;

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

    // Admin login routes are literal Razor page routes under /raytha (see Areas/Admin/Pages/Login),
    // built by path rather than by page name so they survive the admin UI moving to the SPA.
    public string AdminLoginUrl(string returnUrl = "") =>
        AdminPath("/raytha/login", returnUrl);

    public string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") =>
        AdminPath($"/raytha/login/magic-link/complete/{Uri.EscapeDataString(token)}", returnUrl);

    public string AdminForgotPasswordCompleteUrl(string token) =>
        AdminPath($"/raytha/login/forgot-password/complete/{Uri.EscapeDataString(token)}");

    private string AdminPath(string path, string returnUrl = "")
    {
        var url = GetBaseUrl() + path;
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        }
        return url;
    }

    public string MediaRedirectToFileUrl(string objectKey) =>
        ResolveUrlByRouteName("mediaitemsredirecttofileurlbyobjectkey", new { objectKey });

    public string MediaPublicFileUrl(string objectKey) =>
        ResolveUrlByRouteName("mediaitemsredirecttofileurlbyobjectkey", new { objectKey });

    public string MediaCloudUploadPresignUrl() =>
        ResolveUrlByRouteName("mediaitemspresignuploadurl", null);

    public string MediaCloudUploadCreateAfterUploadUrl() =>
        ResolveUrlByRouteName("mediaitemscreateafterupload", null);

    public string MediaDirectUploadUrl() =>
        ResolveUrlByRouteName("mediaitemslocalstorageupload", null);

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

    public string GetBaseUrl()
    {
        // Prefer the configured WebsiteUrl for security; fall back to request for dev/unconfigured
        if (!string.IsNullOrEmpty(_currentOrganization.WebsiteUrl))
        {
            var baseUrl = _currentOrganization.WebsiteUrl.TrimEnd('/');
            return $"{baseUrl}{_currentOrganization.PathBase}";
        }

        // Fallback for development or when WebsiteUrl is not configured
        if (_httpContextAccessor.HttpContext == null)
        {
            return $"{_currentOrganization.PathBase}";
        }

        return $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_currentOrganization.PathBase}";
    }

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

    private string ResolveUrlIfHttpContextAccessExists(
        string controller,
        string action,
        object values
    )
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return _generator.GetPathByAction(action, controller, values: values);
        }
        else
        {
            var url = _generator.GetUriByAction(
                _httpContextAccessor.HttpContext,
                action,
                controller,
                values: values
            );
            return url;
        }
    }

    private string ResolveUrlByRouteName(string routeName, object? values)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return _generator.GetPathByName(routeName, values: values) ?? string.Empty;
        }
        else
        {
            return _generator.GetUriByName(
                _httpContextAccessor.HttpContext,
                routeName,
                values: values
            ) ?? string.Empty;
        }
    }

    public string GetSingleSignOnCallbackJwtUrl(
        string area,
        string developerName,
        string signinUrl,
        string returnUrl = ""
    )
    {
        var prefix = string.Equals(area, "Public", StringComparison.OrdinalIgnoreCase)
            ? "/account"
            : "/raytha";
        var callbackUrl = GetBaseUrl() + $"{prefix}/login/jwt/{developerName}";
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
        var prefix = string.Equals(area, "Public", StringComparison.OrdinalIgnoreCase)
            ? "/account"
            : "/raytha";
        var acsUrl = GetBaseUrl() + $"{prefix}/login/saml/{developerName}";
        var samlRequest = SamlUtility.GetSamlRequestAsBase64(acsUrl, samlIdpEntityId);
        var parametersToAdd = new Dictionary<string, string> { { "SAMLRequest", samlRequest } };

        if (!string.IsNullOrEmpty(returnUrl))
            parametersToAdd.Add("RelayState", returnUrl);

        var loginUrl = QueryHelpers.AddQueryString(signinUrl, parametersToAdd);
        return loginUrl;
    }
}
