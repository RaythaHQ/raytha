using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raytha.Web.Middlewares;

namespace Raytha.Architecture.Tests;

[TestFixture]
public class ProxyTrustTests
{
    [TestCase(null, null, 1)]
    [TestCase("", null, 1)]
    [TestCase(" all ", null, 1)]
    [TestCase("ALL", "2", 2)]
    [TestCase(null, " 3 ", 3)]
    public void All_or_unset_trusts_any_peer_for_the_given_hops(string? proxies, string? hops, int expected)
    {
        ProxyTrust.Parse(proxies, hops).Should().Be(new ProxyTrust.AnyPeer(expected));
    }

    [TestCase("none")]
    [TestCase(" NONE ")]
    public void None_ignores_forwarded_headers(string proxies)
    {
        ProxyTrust.Parse(proxies, null).Should().BeOfType<ProxyTrust.Ignored>();
    }

    [Test]
    public void Private_expands_to_loopback_link_local_rfc1918_cgnat_and_ula()
    {
        var listed = (ProxyTrust.Listed)ProxyTrust.Parse("Private", null);

        listed.Networks.Select(n => n.ToString()).Should().Equal(
            "127.0.0.0/8", "::1/128", "169.254.0.0/16", "fe80::/10",
            "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "100.64.0.0/10", "fc00::/7");
        listed.Proxies.Should().BeEmpty();
    }

    [Test]
    public void A_list_mixes_presets_cidrs_and_single_addresses()
    {
        var listed = (ProxyTrust.Listed)ProxyTrust.Parse(
            " private , 10.20.0.0/16,203.0.113.7, 2001:db8::/32, 2001:db8::1, ::ffff:198.51.100.4,",
            null
        );

        listed.Networks.Should().HaveCount(ProxyTrust.PrivateNetworks.Count + 2);
        listed.Proxies.Should().Equal(
            IPAddress.Parse("203.0.113.7"), IPAddress.Parse("2001:db8::1"), IPAddress.Parse("198.51.100.4"));
        listed.Sources.Should().Equal(
            "private (9 networks)", "10.20.0.0/16", "203.0.113.7", "2001:db8::/32", "2001:db8::1", "198.51.100.4");
    }

    [TestCase("all, private", "'all' must be the only value")]
    [TestCase("10.0.0.0/8,none", "'none' must be the only value")]
    [TestCase("cloudflare", "'cloudflare'")]
    [TestCase("private,cloudflare", "'cloudflare'")]
    [TestCase("10.0.0.0/33", "'10.0.0.0/33'")]
    [TestCase("10.20.1.0/16", "'10.20.1.0/16' in TRUSTED_PROXIES has host bits set. Did you mean 10.20.0.0/16?")]
    [TestCase("10", "'10'")]
    [TestCase("10.1", "'10.1'")]
    [TestCase("10.0/8", "'10.0/8'")]
    [TestCase("999.1.1.1", "'999.1.1.1'")]
    [TestCase("2001:db8::/129", "'2001:db8::/129'")]
    public void A_bad_entry_fails_startup_naming_it(string proxies, string expected)
    {
        var parse = () => ProxyTrust.Parse(proxies, null);

        parse.Should().Throw<InvalidOperationException>().WithMessage($"*{expected}*");
    }

    [TestCase("private", "2")]
    [TestCase("10.0.0.0/8", "1")]
    [TestCase("none", "1")]
    public void Hops_only_apply_to_all(string proxies, string hops)
    {
        var parse = () => ProxyTrust.Parse(proxies, hops);

        parse.Should().Throw<InvalidOperationException>().WithMessage("TRUSTED_PROXY_HOPS only applies*");
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("1.5")]
    [TestCase("two")]
    public void Hops_must_be_a_positive_whole_number(string hops)
    {
        var parse = () => ProxyTrust.Parse(null, hops);

        parse.Should().Throw<InvalidOperationException>().WithMessage($"*'{hops}'*");
    }

    [Test]
    public void Describe_names_the_resolved_trust()
    {
        ProxyTrust.Parse(null, null).Describe().Should().Be(
            "Trusting forwarded headers from any peer, 1 hop. Make sure this app is only reachable through your proxy.");
        ProxyTrust.Parse("all", "2").Describe().Should().StartWith("Trusting forwarded headers from any peer, 2 hops.");
        ProxyTrust.Parse("private,10.20.0.0/16", null).Describe().Should().Be(
            "Trusting forwarded headers from: private (9 networks), 10.20.0.0/16.");
        ProxyTrust.Parse("none", null).Describe().Should().Be("Forwarded headers are ignored (TRUSTED_PROXIES=none).");
    }

    [Test]
    public void Options_forward_for_and_proto_only()
    {
        var any = new ForwardedHeadersOptions();
        ProxyTrust.Parse(null, "2").ApplyTo(any);
        any.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        any.ForwardLimit.Should().Be(2);
        any.KnownIPNetworks.Should().BeEmpty();
        any.KnownProxies.Should().BeEmpty();

        var listed = new ForwardedHeadersOptions();
        ProxyTrust.Parse("10.20.0.0/16,203.0.113.7", null).ApplyTo(listed);
        listed.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        listed.ForwardLimit.Should().BeNull();
        listed.KnownIPNetworks.Select(n => n.ToString()).Should().Equal("10.20.0.0/16");
        listed.KnownProxies.Should().Equal(IPAddress.Parse("203.0.113.7"));

        var ignored = new ForwardedHeadersOptions();
        ProxyTrust.Parse("none", null).ApplyTo(ignored);
        ignored.ForwardedHeaders.Should().Be(ForwardedHeaders.None);
    }

    // The real ForwardedHeadersMiddleware, configured exactly as Startup configures it.
    private static async Task<HttpContext> Send(
        string? proxies,
        string? hops,
        string peer,
        string? forwardedFor,
        string? forwardedProto = null,
        string? forwardedHost = null
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [ProxyTrust.ProxiesKey] = proxies, [ProxyTrust.HopsKey] = hops }
            )
            .Build();
        var services = new ServiceCollection().AddLogging().AddRaythaForwardedHeaders(configuration);
        var app = new ApplicationBuilder(services.BuildServiceProvider());
        app.UseRaythaForwardedHeaders();
        app.Run(_ => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("raytha.example");
        if (forwardedFor is not null)
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        if (forwardedProto is not null)
            context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        if (forwardedHost is not null)
            context.Request.Headers["X-Forwarded-Host"] = forwardedHost;
        await app.Build()(context);
        return context;
    }

    [Test]
    public async Task All_one_hop_takes_what_the_proxy_appended_not_the_forged_leftmost_entry()
    {
        var context = await Send(null, null, "203.0.113.9", "6.6.6.6, 198.51.100.7", "https");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"));
        context.Request.Scheme.Should().Be("https");
    }

    [Test]
    public async Task All_two_hops_walks_a_cdn_in_front_of_a_platform_proxy()
    {
        var context = await Send("all", "2", "10.0.0.2", "6.6.6.6, 198.51.100.7, 172.70.1.1");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"));
    }

    [Test]
    public async Task X_forwarded_host_is_never_honored()
    {
        var context = await Send(null, null, "10.0.0.2", "198.51.100.7", "https", "evil.example");

        context.Request.Host.Value.Should().Be("raytha.example");
    }

    [Test]
    public async Task A_list_stops_at_the_first_untrusted_hop()
    {
        var single = await Send("private", null, "10.0.0.2", "6.6.6.6, 198.51.100.7");
        single.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"));

        var chain = await Send("private", null, "10.0.0.2", "6.6.6.6, 198.51.100.7, 10.0.0.3, 10.0.0.4");
        chain.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"));
    }

    [TestCase("private")]
    [TestCase("10.20.0.0/16")]
    [TestCase("none")]
    public async Task A_peer_outside_the_trust_cannot_forge_its_address_or_scheme(string proxies)
    {
        var context = await Send(proxies, null, "203.0.113.9", "6.6.6.6", "https");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.9"));
        context.Request.Scheme.Should().Be("http");
    }

    [Test]
    public async Task None_ignores_even_a_private_peer()
    {
        var context = await Send("none", null, "10.0.0.2", "198.51.100.7", "https");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("10.0.0.2"));
        context.Request.Scheme.Should().Be("http");
    }

    [TestCase("private", "::ffff:10.0.0.2")]
    [TestCase("10.20.0.0/16", "::ffff:10.20.1.1")]
    [TestCase("192.0.2.10", "::ffff:192.0.2.10")]
    public async Task An_ipv4_mapped_ipv6_peer_matches_ipv4_trust(string proxies, string peer)
    {
        var context = await Send(proxies, null, peer, "198.51.100.7");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("198.51.100.7"));
    }

    [Test]
    public async Task Ipv6_proxies_and_clients_work()
    {
        var context = await Send("fd00::/8", null, "fd00::2", "2001:db8::7");

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("2001:db8::7"));
    }
}
