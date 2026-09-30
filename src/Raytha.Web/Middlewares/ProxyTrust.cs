using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IPNetwork = System.Net.IPNetwork;

namespace Raytha.Web.Middlewares;

/// <summary>
/// Whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> to believe, parsed once from
/// <c>TRUSTED_PROXIES</c> and <c>TRUSTED_PROXY_HOPS</c>. <c>X-Forwarded-Host</c> is never honored.
/// </summary>
public abstract record ProxyTrust
{
    public const string ProxiesKey = "TRUSTED_PROXIES";
    public const string HopsKey = "TRUSTED_PROXY_HOPS";

    /// <summary><c>none</c>: the connecting peer is the client.</summary>
    public sealed record Ignored : ProxyTrust;

    /// <summary><c>all</c>: any peer is a proxy, for a fixed number of hops.</summary>
    public sealed record AnyPeer(int Hops) : ProxyTrust;

    /// <summary>Named proxies: the walk stops at the first hop that is not one of them.</summary>
    public sealed record Listed(
        IReadOnlyList<IPNetwork> Networks,
        IReadOnlyList<IPAddress> Proxies,
        IReadOnlyList<string> Sources
    ) : ProxyTrust;

    public static readonly IReadOnlyList<IPNetwork> PrivateNetworks =
    [
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("fc00::/7"),
    ];

    public static ProxyTrust Parse(string? proxies, string? hops)
    {
        var entries = (proxies ?? string.Empty).Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        var hopsGiven = !string.IsNullOrWhiteSpace(hops);
        var hopCount = 1;
        if (
            hopsGiven
            && (
                !int.TryParse(hops!.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out hopCount)
                || hopCount < 1
            )
        )
        {
            throw Invalid($"{HopsKey} must be a positive whole number, not '{hops}'.");
        }

        foreach (var solo in new[] { "all", "none" })
        {
            if (entries.Length > 1 && entries.Any(e => e.Equals(solo, StringComparison.OrdinalIgnoreCase)))
                throw Invalid($"'{solo}' must be the only value in {ProxiesKey}.");
        }

        if (entries.Length == 0 || entries[0].Equals("all", StringComparison.OrdinalIgnoreCase))
            return new AnyPeer(hopCount);

        if (hopsGiven)
        {
            throw Invalid(
                $"{HopsKey} only applies when {ProxiesKey} is all or unset. A list of proxies is followed hop by hop until the first address that is not on it."
            );
        }

        if (entries[0].Equals("none", StringComparison.OrdinalIgnoreCase))
            return new Ignored();

        var networks = new List<IPNetwork>();
        var addresses = new List<IPAddress>();
        var sources = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.Equals("private", StringComparison.OrdinalIgnoreCase))
            {
                networks.AddRange(PrivateNetworks);
                sources.Add($"private ({PrivateNetworks.Count} networks)");
            }
            else if (TryParseNetwork(entry, out var network))
            {
                // IPNetwork masks off host bits, so 10.20.1.0/16 would silently trust all of
                // 10.20.0.0/16. Widening trust has to be written down, not inferred.
                if (!network.BaseAddress.Equals(ParsedAddress(entry[..entry.IndexOf('/')])))
                    throw Invalid($"'{entry}' in {ProxiesKey} has host bits set. Did you mean {network}?");
                networks.Add(network);
                sources.Add(network.ToString());
            }
            else if (TryParseAddress(entry, out var address))
            {
                addresses.Add(address);
                sources.Add(address.ToString());
            }
            else
            {
                throw Invalid(
                    $"'{entry}' in {ProxiesKey} is not all, none, private, an IP address, or a CIDR range such as 10.20.0.0/16."
                );
            }
        }
        return new Listed(networks, addresses, sources);
    }

    public string Describe() =>
        this switch
        {
            AnyPeer any =>
                $"Trusting forwarded headers from any peer, {any.Hops} {(any.Hops == 1 ? "hop" : "hops")}. Make sure this app is only reachable through your proxy.",
            Listed listed => $"Trusting forwarded headers from: {string.Join(", ", listed.Sources)}.",
            _ => $"Forwarded headers are ignored ({ProxiesKey}=none).",
        };

    public void ApplyTo(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = this is Ignored
            ? ForwardedHeaders.None
            : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        switch (this)
        {
            case AnyPeer any:
                options.ForwardLimit = any.Hops;
                break;
            case Listed listed:
                foreach (var network in listed.Networks)
                    options.KnownIPNetworks.Add(network);
                foreach (var proxy in listed.Proxies)
                    options.KnownProxies.Add(proxy);
                // Every accepted hop must come from a listed address, so the walk already ends at
                // the first untrusted one. A cap would only cut off a chain of trusted proxies.
                options.ForwardLimit = null;
                break;
        }
    }

    // IPAddress.TryParse accepts shorthand such as "10" (0.0.0.10) and "10.1" (10.0.0.1). An
    // operator who writes that almost certainly meant something else, so IPv4 must be four parts.
    private static bool TryParseAddress(string text, out IPAddress address)
    {
        if (
            !IPAddress.TryParse(text, out address!)
            || (
                address.AddressFamily == AddressFamily.InterNetwork
                && (text.Count(c => c == '.') != 3 || !text.All(c => char.IsAsciiDigit(c) || c == '.'))
            )
            || (address.AddressFamily == AddressFamily.InterNetworkV6 && !text.Contains(':'))
        )
        {
            return false;
        }
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        return true;
    }

    private static IPAddress ParsedAddress(string text) =>
        IPAddress.TryParse(text, out var address) ? address : IPAddress.None;

    private static bool TryParseNetwork(string text, out IPNetwork network)
    {
        network = default;
        var slash = text.IndexOf('/');
        return slash > 0
            && TryParseAddress(text[..slash], out _)
            && IPNetwork.TryParse(text, out network);
    }

    private static InvalidOperationException Invalid(string message) => new(message);
}

public static class ForwardedHeadersSetup
{
    public static IServiceCollection AddRaythaForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var trust = ProxyTrust.Parse(
            configuration[ProxyTrust.ProxiesKey],
            configuration[ProxyTrust.HopsKey]
        );
        services.AddSingleton(trust);
        services.Configure<ForwardedHeadersOptions>(trust.ApplyTo);
        return services;
    }

    /// <summary>
    /// Must stay the parameterless <c>UseForwardedHeaders()</c>, which adds the middleware at most
    /// once per pipeline. <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED=true</c> (App Service on Linux)
    /// adds it earlier from a startup filter; the guard makes this call a no-op then, so one pass
    /// runs, with these options. The overload that takes options has no guard and would add a
    /// second pass that consumes a second hop.
    /// </summary>
    public static IApplicationBuilder UseRaythaForwardedHeaders(this IApplicationBuilder app)
    {
        var trust = app.ApplicationServices.GetRequiredService<ProxyTrust>();
        app
            .ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ForwardedHeadersSetup).FullName!)
            .LogInformation("{ForwardedHeadersTrust}", trust.Describe());
        return trust is ProxyTrust.Ignored ? app : app.UseForwardedHeaders();
    }
}
