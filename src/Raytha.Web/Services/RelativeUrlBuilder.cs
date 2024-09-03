using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Web.Services;

public class RelativeUrlBuilder : IRelativeUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _generator;
    private readonly ICurrentOrganization _currentOrganization;

    public RelativeUrlBuilder(IHttpContextAccessor httpContextAccessor, LinkGenerator generator, ICurrentOrganization currentOrganization)
    {
        _httpContextAccessor = httpContextAccessor;
        _generator = generator;
        _currentOrganization = currentOrganization;
    }

    public string AdminLoginUrl() => ResolveUrlIfHttpContextAccessExists("LoginWithEmailAndPassword", "Login", new { area = "Admin", controller = "Login", action = "LoginWithEmailAndPassword" });

    public string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") => ResolveUrlIfHttpContextAccessExists("LoginWithMagicLinkComplete", "Login", new { area = "Admin", controller = "Login", action = "LoginWithMagicLinkComplete", token, returnUrl });

    public string AdminForgotPasswordCompleteUrl(string token) => ResolveUrlIfHttpContextAccessExists("ForgotPasswordComplete", "Login", new { area = "Admin", controller = "Login", action = "ForgotPasswordComplete", token });

    public string MediaRedirectToFileUrl(string objectKey) => ResolveUrlIfHttpContextAccessExists("RedirectToFileUrlByObjectKey", "MediaItems", new { area = "Admin", controller = "MediaItems", action = "RedirectToFileUrlByObjectKey", objectKey });

    public string MediaFileLocalStorageUrl(string objectKey) => GetBaseUrl() + $"/_static-files/{objectKey}";

    public string UserLoginUrl() => ResolveUrlIfHttpContextAccessExists("LoginWithEmailAndPassword", "Login", new { area = "Public", controller = "Login", action = "LoginWithEmailAndPassword" });

    public string UserLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") => ResolveUrlIfHttpContextAccessExists("LoginWithMagicLinkComplete", "Login", new { area = "Public", controller = "Login", action = "LoginWithMagicLinkComplete", token, returnUrl });

    public string UserForgotPasswordCompleteUrl(string token) => ResolveUrlIfHttpContextAccessExists("ForgotPasswordComplete", "Login", new { area = "Public", controller = "Login", action = "ForgotPasswordComplete", token });
    public string GetBaseUrl() => _httpContextAccessor.HttpContext == null ? $"{_currentOrganization.PathBase}" : $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_currentOrganization.PathBase}";

    private string ResolveUrlIfHttpContextAccessExists(string action, string controller, object values)
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            return _generator.GetPathByAction(action, controller, values: values);
        }
        else
        {
            return _generator.GetUriByPage(_httpContextAccessor.HttpContext, values: values);
        }
    }
}
