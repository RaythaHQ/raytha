using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Raytha.Web.Middlewares;

/// <summary>
/// Sliding-window rate limit on authentication endpoints, keyed by client IP. Applied as a
/// global limiter with path-based partitioning so it covers Razor pages, controllers, and
/// minimal APIs alike without per-endpoint attributes. Everything else is unlimited.
/// </summary>
public static class RateLimiting
{
    public const int DefaultPermitLimit = 30;
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);

    /// <summary>Path prefixes (relative to path base) that are rate limited.</summary>
    public static readonly string[] AuthPathPrefixes =
    {
        "/raytha/api/auth",
        "/raytha/login",
        "/account/login",
    };

    public static bool IsAuthPath(PathString path, string pathBase)
    {
        var value = path.Value ?? string.Empty;
        if (!string.IsNullOrEmpty(pathBase) && value.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase))
        {
            value = value[pathBase.Length..];
        }

        return AuthPathPrefixes.Any(prefix =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        );
    }

    public static IServiceCollection AddRaythaRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var pathBase = configuration["PATHBASE"] ?? string.Empty;
        var permitLimit = int.TryParse(configuration["AUTH_RATE_LIMIT_PER_MINUTE"], out var parsed)
            ? Math.Max(1, parsed)
            : DefaultPermitLimit;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (!IsAuthPath(context.Request.Path, pathBase))
                {
                    return RateLimitPartition.GetNoLimiter("unlimited");
                }

                var clientIp =
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    $"auth:{clientIp}",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = DefaultWindow,
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }
                );
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                if (ExceptionsMiddleware.IsApiPath(http.Request.Path, pathBase))
                {
                    http.Response.ContentType = ExceptionsMiddleware.ProblemJsonContentType;
                    await http.Response.WriteAsync(
                        JsonSerializer.Serialize(
                            new
                            {
                                type = "https://httpstatuses.io/429",
                                title = "Too many requests",
                                status = 429,
                                detail = "Too many login attempts. Please try again shortly.",
                                instance = http.Request.Path.Value,
                                success = false,
                                error = "Too many login attempts. Please try again shortly.",
                            }
                        ),
                        cancellationToken
                    );
                }
                else
                {
                    http.Response.ContentType = "text/plain";
                    await http.Response.WriteAsync(
                        "Too many login attempts. Please try again shortly.",
                        cancellationToken
                    );
                }
            };
        });

        return services;
    }
}
