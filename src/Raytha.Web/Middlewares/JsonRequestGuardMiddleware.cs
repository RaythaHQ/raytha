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
                if (!RequiresJson(context.Request) || IsJson(context.Request))
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
}
