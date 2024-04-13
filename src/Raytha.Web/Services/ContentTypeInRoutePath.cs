using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Web.Utils;
using System;

namespace Raytha.Web.Services;

public class ContentTypeInRoutePath : IContentTypeInRoutePath
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ContentTypeInRoutePath(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string ContentTypeDeveloperName => ((string)_httpContextAccessor.HttpContext.Request.RouteValues[RouteConstants.CONTENT_TYPE_DEVELOPER_NAME]).ToDeveloperName();

    public bool ValidateContentTypeInRoutePathMatchesValue(string developerName, bool throwExceptionOnFailure = true)
    {
        //contentType will exist outside admin or the api
        var path = _httpContextAccessor.HttpContext.Request.Path.Value.ToLower();
        if (!path.StartsWith("/raytha") || path.StartsWith("/raytha/functions/execute/"))
            return true;

        bool isMatch = ContentTypeDeveloperName == developerName;
        return isMatch
            ? true
            : throwExceptionOnFailure
            ? throw new UnauthorizedAccessException("Content type in route path does not match content item's content type.")
            : false;
    }
}
