using System;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Utils;

namespace Raytha.Web.Services;

public class ContentTypeInRoutePath : IContentTypeInRoutePath
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContentTypeInRoutePath(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string ContentTypeDeveloperName =>
        (
            (string)
                _httpContextAccessor.HttpContext.Request.RouteValues[
                    RouteConstants.CONTENT_TYPE_DEVELOPER_NAME
                ]
        ).ToDeveloperName();

    public bool ValidateContentTypeInRoutePathMatchesValue(
        string developerName,
        bool throwExceptionOnFailure = true
    )
    {
        // Content-type routes under /raytha carry the developer name. A route that does not
        // (template preview, functions) has nothing to mismatch.
        var httpContext = _httpContextAccessor.HttpContext;
        var path = httpContext.Request.Path.Value?.ToLower() ?? string.Empty;
        if (!path.StartsWith("/raytha") || path.StartsWith("/raytha/functions/execute/"))
            return true;

        if (
            !httpContext.Request.RouteValues.TryGetValue(
                RouteConstants.CONTENT_TYPE_DEVELOPER_NAME,
                out var raw
            )
            || raw is not string routeName
            || string.IsNullOrEmpty(routeName)
        )
        {
            return true;
        }

        bool isMatch = routeName.ToDeveloperName() == developerName;
        return isMatch ? true
            : throwExceptionOnFailure
                ? throw new UnauthorizedAccessException(
                    "Content type in route path does not match content item's content type."
                )
            : false;
    }
}
