using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Raytha.Application;
using Raytha.Application.Common.Utils;
using Raytha.Web.Middlewares;
using System.IO;

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
        services.AddApplicationServices();
        services.AddInfrastructureServices(Configuration);
        services.AddWebUIServices();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        string pathBase = Configuration["PATHBASE"] ?? string.Empty;
        app.UsePathBase(new PathString(pathBase));

        app.UseExceptionHandler(ExceptionsMiddleware.ErrorHandler(pathBase));

        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto
        });

        app.UseStaticFiles();

        var fileStorageProvider = Configuration[FileStorageUtility.CONFIG_NAME].IfNullOrEmpty(FileStorageUtility.LOCAL).ToLower();
        var localStorageDirectory = Configuration[FileStorageUtility.LOCAL_DIRECTORY_CONFIG_NAME].IfNullOrEmpty(FileStorageUtility.DEFAULT_LOCAL_DIRECTORY);
        if (fileStorageProvider == FileStorageUtility.LOCAL)
        {
            var fullPath = Path.Combine(env.ContentRootPath, localStorageDirectory);
            Directory.CreateDirectory(fullPath);        
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(
                    fullPath),
                    RequestPath = new PathString("/_static-files")
            });
        }

        app.UseSwagger(c =>
        {
            c.RouteTemplate = "raytha/api/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{pathBase}/raytha/api/v1/swagger.json", "Raytha API - V1");
            c.RoutePrefix = $"raytha/api";
        });

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}