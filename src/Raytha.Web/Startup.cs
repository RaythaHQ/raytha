using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
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
                ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
        });
        services.AddApplicationServices();
        services.AddInfrastructureServices(Configuration);
        services.AddWebUIServices();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
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

        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseStaticFiles();

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
                }
            );
        }

        /*
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "raytha/api/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{pathBase}/raytha/api/v1/swagger.json", "Raytha API - V1");
            c.RoutePrefix = $"raytha/api";
        });
        */

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
