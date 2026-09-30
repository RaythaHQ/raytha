using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Services;

namespace RaythaZero.Web.Services;

public class RelativeUrlBuilder : IRelativeUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _generator;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IHostEnvironment _environment;

    public RelativeUrlBuilder(
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator generator,
        ICurrentOrganization currentOrganization,
        IHostEnvironment environment
    )
    {
        _httpContextAccessor = httpContextAccessor;
        _generator = generator;
        _currentOrganization = currentOrganization;
        _environment = environment;
    }

    // Login routes are literal (Razor pages under /raytha, MVC attribute routes under /account),
    // built by path rather than by page or action name so they survive the admin UI moving to the SPA.
    public string AdminLoginUrl(string returnUrl = "") => SitePath("/raytha/login", returnUrl);

    public string AdminForgotPasswordCompleteUrl(string token) =>
        SitePath($"/raytha/login/forgot-password/complete/{Uri.EscapeDataString(token)}");

    public string UserLoginUrl(string returnUrl = "") => SitePath("/account/login", returnUrl);

    public string UserForgotPasswordCompleteUrl(string token) =>
        SitePath($"/account/login/forgot-password/complete/{Uri.EscapeDataString(token)}");

    private string SitePath(string path, string returnUrl = "")
    {
        var url = GetBaseUrl() + path;
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url = QueryHelpers.AddQueryString(url, "returnUrl", returnUrl);
        }
        return url;
    }

    public string MediaRedirectToFileUrl(string objectKey) =>
        GetSiteRoot() + MediaRedirectToFilePath(objectKey);

    public string MediaRedirectToFilePath(string objectKey) =>
        _httpContextAccessor.HttpContext is { } httpContext
            ? _generator.GetPathByName(httpContext, "mediaitemsredirecttofileurlbyobjectkey", new { objectKey })
                ?? string.Empty
            : _currentOrganization.PathBase
                + _generator.GetPathByName("mediaitemsredirecttofileurlbyobjectkey", new { objectKey });

    public string MediaFileLocalStorageUrl(string objectKey) =>
        GetBaseUrl() + $"/_static-files/{objectKey}";

    public string GetBaseUrl() => GetSiteRoot() + _currentOrganization.PathBase;

    public string GetSiteRoot() =>
        SiteRoot(
            _currentOrganization.WebsiteUrl,
            _currentOrganization.PathBase,
            _environment.IsDevelopment() && _httpContextAccessor.HttpContext is { } httpContext
                ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
                : null
        );

    /// <summary>
    /// WebsiteUrl without a trailing slash, and without the path base when the operator included
    /// it, so appending the path base or a stored path never doubles it. Without a WebsiteUrl it is
    /// the request origin in Development and empty otherwise, which keeps links root-relative
    /// instead of trusting a Host header.
    /// </summary>
    public static string SiteRoot(string? websiteUrl, string? pathBase, string? developmentRequestOrigin)
    {
        var root = (websiteUrl ?? string.Empty).Trim().TrimEnd('/');
        if (root.Length == 0)
        {
            return developmentRequestOrigin ?? string.Empty;
        }

        var basePath = (pathBase ?? string.Empty).TrimEnd('/');
        if (
            basePath.Length > 0
            && Uri.TryCreate(root, UriKind.Absolute, out var uri)
            && uri.AbsolutePath.TrimEnd('/').EndsWith(basePath, StringComparison.OrdinalIgnoreCase)
            && root.EndsWith(basePath, StringComparison.OrdinalIgnoreCase)
        )
        {
            root = root[..^basePath.Length];
        }
        return root;
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
