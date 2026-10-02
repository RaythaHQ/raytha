using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raytha.Web.Middlewares;

namespace Raytha.Architecture.Tests;

/// <summary>
/// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true makes the web host defaults add a forwarded-headers
/// pass from a startup filter, with options of their own. These build the host the way Program
/// does and run the pipeline through its startup filters, as the web host would.
/// </summary>
[TestFixture]
[NonParallelizable]
public class ForwardedHeadersSinglePassTests
{
    private const string Switch = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";
    private const string ForgedChain = "6.6.6.6, 198.51.100.7";
    private static readonly IPAddress ProxyPeer = IPAddress.Parse("10.0.0.2");

    [TestCase(null)]
    [TestCase("true")]
    public async Task Raytha_consumes_exactly_one_hop_with_or_without_the_framework_switch(
        string? switchValue
    )
    {
        var client = await ClientSeenByApp(switchValue, trustedProxies: null, ProxyPeer);

        client.Should().Be("198.51.100.7");
    }

    [TestCase(null)]
    [TestCase("true")]
    public async Task The_framework_switch_does_not_replace_raytha_trust_settings(
        string? switchValue
    )
    {
        var outsider = IPAddress.Parse("203.0.113.9");

        var client = await ClientSeenByApp(switchValue, trustedProxies: "10.20.0.0/16", outsider);

        client.Should().Be("203.0.113.9");
    }

    [Test]
    public async Task The_parameterless_middleware_is_added_once_however_often_it_is_called()
    {
        var client = await ClientSeenByApp(
            switchValue: null,
            trustedProxies: null,
            ProxyPeer,
            app => app.UseForwardedHeaders().UseForwardedHeaders()
        );

        client.Should().Be("198.51.100.7", "UseRaythaForwardedHeaders relies on this guard");
    }

    [Test]
    public async Task The_framework_switch_adds_a_pass_of_its_own()
    {
        var client = await ClientSeenByApp("true", trustedProxies: null, ProxyPeer, _ => { });

        client.Should().Be("198.51.100.7", "otherwise the switch tests above prove nothing");
    }

    [Test]
    public async Task The_options_overload_is_not_guarded_and_consumes_a_second_hop()
    {
        var client = await ClientSeenByApp(
            "true",
            trustedProxies: null,
            ProxyPeer,
            app =>
                app.UseForwardedHeaders(
                    app.ApplicationServices.GetRequiredService<
                        Microsoft.Extensions.Options.IOptions<ForwardedHeadersOptions>
                    >().Value
                )
        );

        client.Should().Be("6.6.6.6", "this is the forged entry a second pass would accept");
    }

    private static async Task<string> ClientSeenByApp(
        string? switchValue,
        string? trustedProxies,
        IPAddress peer,
        Action<IApplicationBuilder>? useForwardedHeaders = null
    )
    {
        var previous = Environment.GetEnvironmentVariable(Switch);
        Environment.SetEnvironmentVariable(Switch, switchValue);
        try
        {
            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { [ProxyTrust.ProxiesKey] = trustedProxies }
                    )
                )
                .ConfigureWebHostDefaults(web =>
                {
                    web.ConfigureServices(
                        (context, services) =>
                            services.AddRaythaForwardedHeaders(context.Configuration)
                    );
                    web.Configure(_ => { });
                })
                .Build();

            Action<IApplicationBuilder> configure = app =>
            {
                (useForwardedHeaders ?? (a => a.UseRaythaForwardedHeaders()))(app);
                app.Run(context =>
                    context.Response.WriteAsync(context.Connection.RemoteIpAddress!.ToString())
                );
            };
            foreach (var filter in host.Services.GetServices<IStartupFilter>().Reverse())
            {
                configure = filter.Configure(configure);
            }
            var builder = new ApplicationBuilder(host.Services);
            configure(builder);
            var pipeline = builder.Build();

            var context = new DefaultHttpContext { RequestServices = host.Services };
            context.Request.Host = new HostString("localhost");
            context.Request.Headers["X-Forwarded-For"] = ForgedChain;
            context.Connection.RemoteIpAddress = peer;
            context.Response.Body = new MemoryStream();
            await pipeline(context);
            context.Response.Body.Position = 0;
            return await new StreamReader(context.Response.Body).ReadToEndAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Switch, previous);
        }
    }
}
