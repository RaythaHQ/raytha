using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Raytha.Web.Middlewares;

/// <summary>
/// CSRF guard for the cookie-authenticated admin API. Browsers can only send a cross-site
/// POST without a CORS preflight as a form or <c>text/plain</c>; requiring
/// <c>application/json</c> on mutating requests rejects those before any handler runs, which
/// is why these endpoints skip antiforgery tokens.
/// </summary>
public static class JsonRequestGuardMiddleware
{
    private static readonly string[] GuardedPrefixes = ["/raytha/api/admin", "/raytha/api/auth"];

    public static IApplicationBuilder UseAdminApiJsonGuard(this IApplicationBuilder app)
    {
        return app.Use(
            async (context, next) =>
            {
                if (!RequiresJson(context.Request) || IsJson(context.Request) || AllowEmptyPost(context.Request))
                {
                    await next();
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(
                        new
                        {
                            type = "https://httpstatuses.io/415",
                            title = "Unsupported media type",
                            status = StatusCodes.Status415UnsupportedMediaType,
                            detail = "Mutating requests to the admin API must send Content-Type: application/json.",
                        }
                    )
                );
            }
        );
    }

    private static bool RequiresJson(HttpRequest request)
    {
        // PUT/PATCH/DELETE already force a CORS preflight; POST is the one a form can forge.
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method))
        {
            return false;
        }

        foreach (var prefix in GuardedPrefixes)
        {
            if (request.Path.StartsWithSegments(prefix))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsJson(HttpRequest request)
    {
        return MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType)
            && (
                mediaType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || mediaType.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase)
            );
    }

    /// <summary>
    /// A POST with no body and no content type is how curl and API clients call endpoints that
    /// take no input. A cross-site browser request sets <c>Sec-Fetch-Site: cross-site</c> (or a
    /// form content type), so it still gets the 415. A form cannot omit its content type.
    /// </summary>
    private static bool AllowEmptyPost(HttpRequest request)
    {
        if (request.ContentLength is > 0 || !string.IsNullOrEmpty(request.ContentType))
        {
            return false;
        }

        var fetchSite = request.Headers["Sec-Fetch-Site"].ToString();
        return !fetchSite.Equals("cross-site", StringComparison.OrdinalIgnoreCase);
    }
}
