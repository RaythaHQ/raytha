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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raytha.Application;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Infrastructure.Persistence;
using Raytha.Web.Middlewares;
using Scalar.AspNetCore;

namespace Raytha.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

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
        services.AddWebUIServices();
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

        app.UseStaticFiles();

        // Security: Add a small set of conservative security headers to all responses to reduce
        // common classes of browser-based attacks (MIME sniffing, clickjacking, and referrer leakage)
        // without constraining existing content or introducing a breaking Content-Security-Policy.
        app.Use(
            async (context, next) =>
            {
                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
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

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
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

            endpoints.MapHealthChecks(
                "/healthz",
                new HealthCheckOptions
                {
                    ResponseWriter = async (ctx, report) =>
                    {
                        ctx.Response.ContentType = "application/json";

                        var currentVersion =
                            ctx.RequestServices.GetRequiredService<ICurrentVersion>();

                        var json = JsonSerializer.Serialize(
                            new
                            {
                                version = currentVersion.Version,
                                status = report.Status.ToString(),
                                checks = report.Entries.Select(e => new
                                {
                                    name = e.Key,
                                    status = e.Value.Status.ToString(),
                                    error = e.Value.Exception?.Message,
                                    duration = e.Value.Duration.ToString(),
                                }),
                            },
                            new JsonSerializerOptions { WriteIndented = true }
                        );
                        await ctx.Response.WriteAsync(json);
                    },
                }
            );
        });

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
            }
        }
    }
}
