using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mediator;
using Raytha.Application;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.Themes.Commands;
using Raytha.Infrastructure.Health;
using Raytha.Infrastructure.Persistence;
using Raytha.Web.AdminSpa;
using Raytha.Web.Areas.Admin.Api;
using Raytha.Web.Areas.Admin.Endpoints;
using Raytha.Web.Middlewares;
using Scalar.AspNetCore;

namespace Raytha.Web;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedHost;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        services.AddApplicationServices();
        services.AddInfrastructureServices(Configuration);
        services.AddWebUIServices(Environment);
        services.AddRaythaRateLimiting(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Console.WriteLine(
            $"[Startup] Environment: {env.EnvironmentName}, IsDevelopment: {env.IsDevelopment()}"
        );

        string pathBase = Configuration["PATHBASE"] ?? string.Empty;
        app.UsePathBase(new PathString(pathBase));
        app.UseForwardedHeaders();
        app.UseExceptionHandler(
            new ExceptionHandlerOptions
            {
                ExceptionHandler = ExceptionsMiddleware.ErrorHandlerDelegate(pathBase, env),
                AllowStatusCode404Response = true,
            }
        );
        app.UseStatusCodePagesWithReExecute($"{pathBase}/raytha/error/{{0}}");

        bool enforceHttps = Convert.ToBoolean(Configuration["ENFORCE_HTTPS"] ?? "true");
        if (!env.IsDevelopment() && enforceHttps)
        {
            // Security: Enforce HTTPS and strict transport security in non-development environments
            // to prevent protocol downgrade and cookie hijacking; this relies on forwarded headers
            // when running behind a reverse proxy and preserves existing development behavior.
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // Development: hand SPA navigations and assets under /raytha to Vite before the stale
        // bundle in wwwroot/raytha or a Razor page can answer. No-op outside Development.
        app.UseAdminSpaDevProxy(env);

        app.UseStaticFiles();

        // Security: Add a small set of conservative security headers to all responses to reduce
        // common classes of browser-based attacks (MIME sniffing, clickjacking, and referrer leakage)
        // without constraining existing content or introducing a breaking Content-Security-Policy.
        // Admin (/raytha) responses must never be framed at all; public pages keep SAMEORIGIN so
        // site builders can still embed their own content.
        app.Use(
            async (context, next) =>
            {
                // UsePathBase strips the prefix, but tolerate either form.
                var isAdminPath =
                    context.Request.Path.StartsWithSegments(
                        "/raytha",
                        StringComparison.OrdinalIgnoreCase
                    )
                    || (
                        !string.IsNullOrEmpty(pathBase)
                        && context.Request.Path.StartsWithSegments(
                            $"{pathBase}/raytha",
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd(
                    "X-Frame-Options",
                    isAdminPath ? "DENY" : "SAMEORIGIN"
                );
                context.Response.Headers.TryAdd(
                    "Referrer-Policy",
                    "strict-origin-when-cross-origin"
                );
                await next();
            }
        );

        var fileStorageProvider = Configuration[FileStorageUtility.CONFIG_NAME]
            .IfNullOrEmpty(FileStorageUtility.LOCAL)
            .ToLower();
        var localStorageDirectory = Configuration[FileStorageUtility.LOCAL_DIRECTORY_CONFIG_NAME]
            .IfNullOrEmpty(FileStorageUtility.DEFAULT_LOCAL_DIRECTORY);
        if (fileStorageProvider == FileStorageUtility.LOCAL)
        {
            var fullPath = Path.Combine(env.ContentRootPath, localStorageDirectory);
            Directory.CreateDirectory(fullPath);
            app.UseStaticFiles(
                new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(fullPath),
                    RequestPath = new PathString("/_static-files"),
                    OnPrepareResponse = ctx =>
                    {
                        // Security: Add restrictive CSP headers to user-uploaded files to prevent
                        // malicious SVGs or other files from executing inline scripts. This blocks
                        // XSS attacks via uploaded SVG files while allowing normal display.
                        ctx.Context.Response.Headers.TryAdd(
                            "Content-Security-Policy",
                            "default-src 'none'; img-src 'self' data:; style-src 'unsafe-inline'; sandbox"
                        );
                    },
                }
            );
        }

        app.UseAdminApiJsonGuard();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapMediaItemsEndpoints();
            endpoints.MapAdminApi();
            endpoints.MapAdminSpaFallback(env);
            endpoints.MapOpenApi("/raytha/api/{documentName}/swagger.json");

            endpoints.MapScalarApiReference(
                "/raytha/api",
                options =>
                {
                    options
                        .WithTitle("Raytha API")
                        .ForceDarkMode()
                        .WithClassicLayout()
                        .ExpandAllTags();
                    options.WithOpenApiRoutePattern("/raytha/api/{documentName}/swagger.json");
                    options
                        .AddPreferredSecuritySchemes("ApiKey")
                        .AddApiKeyAuthentication(
                            "X-API-KEY",
                            (scheme) =>
                            {
                                scheme.Name = "ApiKey";
                                scheme.Name = "X-API-KEY";
                            }
                        );
                }
            );

            // Liveness: the process is up and can serve a response. Runs no checks so a
            // degraded dependency never causes an orchestrator to restart a healthy pod.
            endpoints.MapHealthChecks(
                "/healthz",
                new HealthCheckOptions
                {
                    Predicate = _ => false,
                    ResponseWriter = WriteHealthResponse,
                }
            );

            // Readiness: Postgres and the file storage provider must both answer.
            endpoints.MapHealthChecks(
                "/healthz/ready",
                new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
                    ResponseWriter = WriteHealthResponse,
                }
            );
        });

        ApplyPendingMigrationsIfConfigured(app);
    }

    private static readonly JsonSerializerOptions HealthJsonOptions = new()
    {
        WriteIndented = true,
    };

    private static Task WriteHealthResponse(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json";

        var currentVersion = ctx.RequestServices.GetRequiredService<ICurrentVersion>();
        var environment = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();

        var json = JsonSerializer.Serialize(
            new
            {
                version = currentVersion.Version,
                environment = environment.EnvironmentName,
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    error = e.Value.Exception?.Message,
                    duration = e.Value.Duration.ToString(),
                    data = e.Value.Data.Count > 0 ? e.Value.Data : null,
                }),
            },
            HealthJsonOptions
        );
        return ctx.Response.WriteAsync(json);
    }

    private void ApplyPendingMigrationsIfConfigured(IApplicationBuilder app)
    {
        bool applyMigrationsOnStartup = Convert.ToBoolean(
            Configuration["APPLY_PENDING_MIGRATIONS"] ?? "false"
        );
        if (applyMigrationsOnStartup)
        {
            using (
                var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope()
            )
            {
                scope.ServiceProvider.GetRequiredService<RaythaDbContext>().Database.Migrate();
                scope
                    .ServiceProvider.GetRequiredService<ISender>()
                    .Send(new EnsureDefaultThemeContent.Command())
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}
