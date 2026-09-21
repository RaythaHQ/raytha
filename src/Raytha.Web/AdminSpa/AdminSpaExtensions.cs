using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace Raytha.Web.AdminSpa;

/// <summary>
/// Serves the React admin app at <c>/raytha</c>. In Development, requests are proxied to the Vite
/// dev server (HMR included, WebSockets and all); otherwise the published bundle is served from
/// <c>wwwroot/raytha</c> with an <c>index.html</c> fallback for client-side routes.
///
/// While Razor Pages still live under <c>/raytha</c>, the Development proxy is authoritative for
/// every <c>/raytha</c> path except the server-side ones in <see cref="ServerPathPrefixes"/>; the
/// production fallback only runs for paths no other endpoint matched.
/// </summary>
public static class AdminSpaExtensions
{
    public const string BasePath = "/raytha";
    public const string DefaultDevServerUrl = "http://localhost:5203";

    /// <summary>
    /// Paths under <c>/raytha</c> that must keep hitting the server: JSON APIs, uploads, SSO
    /// callbacks, links sent by email, and anything that sets a cookie then redirects.
    /// </summary>
    public static readonly string[] ServerPathPrefixes =
    [
        $"{BasePath}/api",
        $"{BasePath}/media-items",
        $"{BasePath}/functions/execute",
        $"{BasePath}/themes/export",
        $"{BasePath}/login/sso",
        $"{BasePath}/login/jwt",
        $"{BasePath}/login/saml",
        $"{BasePath}/login/magic-link/complete",
        $"{BasePath}/login/forgot-password/complete",
        $"{BasePath}/login-redirect",
        $"{BasePath}/logout",
        $"{BasePath}/error",
    ];

    public static string DevServerUrl(IConfiguration configuration) =>
        configuration["AdminSpa:DevServerUrl"] ?? configuration["ADMIN_SPA_DEV_SERVER_URL"] ?? DefaultDevServerUrl;

    public static bool IsServerPath(PathString path)
    {
        foreach (var prefix in ServerPathPrefixes)
        {
            if (path.StartsWithSegments(prefix))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsSpaPath(PathString path) => path.StartsWithSegments(BasePath) && !IsServerPath(path);

    public static IServiceCollection AddAdminSpa(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddSingleton<AdminSpaProxyState>();

        if (environment.IsDevelopment())
        {
            services.AddHttpForwarder();
            services.AddHostedService<ViteDevServerHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Development only. Place before <c>UseStaticFiles</c> and <c>UseRouting</c> so Vite, not the
    /// stale bundle in <c>wwwroot/raytha</c> or a Razor page, answers SPA navigations and assets.
    /// </summary>
    public static IApplicationBuilder UseAdminSpaDevProxy(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return app;
        }

        var state = app.ApplicationServices.GetRequiredService<AdminSpaProxyState>();
        var forwarder = app.ApplicationServices.GetRequiredService<IHttpForwarder>();
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Raytha.AdminSpa");
        var devServerUrl = DevServerUrl(configuration);

        var httpClient = new HttpMessageInvoker(
            new SocketsHttpHandler
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
            }
        );
        var requestConfig = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromMinutes(5) };

        app.Use(
            async (context, next) =>
            {
                if (!state.ViteReady || !IsSpaPath(context.Request.Path))
                {
                    await next();
                    return;
                }

                var error = await forwarder.SendAsync(context, devServerUrl, httpClient, requestConfig);
                if (error != ForwarderError.None)
                {
                    var feature = context.GetForwarderErrorFeature();
                    logger.LogWarning(
                        feature?.Exception,
                        "Proxying {Path} to Vite at {Url} failed: {Error}",
                        context.Request.Path,
                        devServerUrl,
                        error
                    );
                }
            }
        );

        return app;
    }

    /// <summary>
    /// SPA fallback for client-side routes. Mapped as an ordinary GET on <c>/raytha/{**path}</c>
    /// rather than <c>MapFallback</c>: route precedence puts it below every Razor page, controller
    /// and minimal API with a literal segment, but above the public site's <c>{*route}</c>
    /// catch-all, which would otherwise swallow SPA deep links. Unmatched server-side paths still 404.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminSpaFallback(this IEndpointRouteBuilder endpoints, IWebHostEnvironment environment)
    {
        var indexPath = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "raytha", "index.html");

        RequestDelegate serveIndex = async context =>
        {
            if (IsServerPath(context.Request.Path) || !File.Exists(indexPath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache, no-store";
            await context.Response.SendFileAsync(indexPath);
        };

        // `{**path}` does not match the bare prefix, so /raytha (the dashboard) needs its own route.
        endpoints.MapGet(BasePath, serveIndex).WithDisplayName("Admin SPA").AllowAnonymous();
        endpoints.MapGet($"{BasePath}/{{**path}}", serveIndex).WithDisplayName("Admin SPA").AllowAnonymous();

        return endpoints;
    }
}
