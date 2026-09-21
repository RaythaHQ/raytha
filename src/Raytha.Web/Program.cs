using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Raytha.Web.Observability;

namespace Raytha.Web;

public class Program
{
    public static void Main(string[] args)
    {
        Env.TraversePath().Load();

        var host = CreateHostBuilder(args).Build();
        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseRaythaObservability()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseRaythaSentry();
                webBuilder.UseStartup<Startup>();
                // 0.0.0.0 so a Tailscale (or LAN) address can reach the dev server.
                // localhost still works. Override with ASPNETCORE_URLS.
                webBuilder.UseUrls(
                    Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                        ?? "http://0.0.0.0:5200"
                );
            });
}
