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
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
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

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "/{*route}",
                defaults: new { area = "Public", controller = "Main", action = "Index" });
        });
    }
}
