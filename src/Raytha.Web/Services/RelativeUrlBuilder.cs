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

    public string AdminLoginUrl() => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Admin", controller = "Login", action = "LoginWithEmailAndPassword" });

    public string AdminLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Admin", controller = "Login", action = "LoginWithMagicLinkComplete", token, returnUrl });

    public string AdminForgotPasswordCompleteUrl(string token) => _generator.GetUriByPage(_httpContextAccessor.HttpContext, 
                values: new { area = "Admin", controller = "Login", action = "ForgotPasswordComplete", token });

    public string MediaRedirectToFileUrl(string objectKey) => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Admin", controller = "MediaItems", action = "RedirectToFileUrlByObjectKey", objectKey });

    public string MediaFileLocalStorageUrl(string objectKey) => $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}{_currentOrganization.PathBase}/_static-files/{objectKey}";

    public string UserLoginUrl() => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Public", controller = "Login", action = "LoginWithEmailAndPassword" });

    public string UserLoginWithMagicLinkCompleteUrl(string token, string returnUrl = "") => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Public", controller = "Login", action = "LoginWithMagicLinkComplete", token, returnUrl });

    public string UserForgotPasswordCompleteUrl(string token) => _generator.GetUriByPage(_httpContextAccessor.HttpContext,
                values: new { area = "Public", controller = "Login", action = "ForgotPasswordComplete", token });
}
