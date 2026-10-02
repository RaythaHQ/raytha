using System.Net;
using System.Net.Sockets;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Validates URLs for safe server-side fetching to prevent SSRF attacks.
/// Can be configured to allow internal URLs for development environments.
/// </summary>
public static class SafeUrlValidator
{
    /// <summary>
    /// Validates that a URL is safe for server-side fetching.
    /// When allowInternal is false, blocks localhost, private IPs, and cloud metadata endpoints.
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <param name="allowInternal">If true, allows internal/localhost URLs (for development)</param>
    /// <param name="error">Error message if validation fails</param>
    /// <returns>True if URL is safe to fetch, false otherwise</returns>
    public static bool IsSafeUrl(string url, bool allowInternal, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL cannot be empty.";
            return false;
        }

        // Parse the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            error = "Invalid URL format.";
            return false;
        }

        // Only allow http and https schemes
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            error = $"URL scheme '{uri.Scheme}' is not allowed. Only http and https are permitted.";
            return false;
        }

        // If we allow internal URLs (development mode), skip the rest of the checks
        if (allowInternal)
        {
            return true;
        }

        // Check for blocked hostnames
        var host = uri.Host.ToLowerInvariant();

        if (IsBlockedHostname(host))
        {
            error = $"URL host '{host}' is not allowed for security reasons.";
            return false;
        }

        // Resolve the hostname and check if it resolves to a blocked IP
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                if (IsBlockedIpAddress(address))
                {
                    error = $"URL resolves to a blocked IP address for security reasons.";
                    return false;
                }
            }
        }
        catch (SocketException)
        {
            // If DNS resolution fails, allow the request to proceed
            // The actual HTTP request will fail with a more appropriate error
        }

        return true;
    }

    /// <summary>
    /// A primary handler that checks the address it actually connects to, so redirects and
    /// DNS answers that change between validation and connection cannot reach an internal host.
    /// </summary>
    public static SocketsHttpHandler CreateHandler(bool allowInternal) =>
        new()
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
                if (!allowInternal && addresses.Any(IsBlockedIpAddress))
                {
                    throw new HttpRequestException(
                        $"Request to {host} blocked because it resolves to an internal network address. Set ALLOW_INTERNAL_URL_IMPORTS=true to allow it."
                    );
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        };

    private static bool IsBlockedHostname(string host)
    {
        // Block localhost variations
        if (host == "localhost" || host == "localhost.localdomain")
            return true;

        // Block common cloud metadata hostnames
        if (
            host == "metadata.google.internal"
            || host == "metadata.goog"
            || host.EndsWith(".internal")
        )
            return true;

        return false;
    }

    private static bool IsBlockedIpAddress(IPAddress address)
    {
        // Block IPv4 loopback (127.0.0.0/8)
        if (IPAddress.IsLoopback(address))
            return true;

        // Block IPv6 loopback (::1)
        if (address.Equals(IPAddress.IPv6Loopback))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // Block 10.0.0.0/8 (private)
            if (bytes[0] == 10)
                return true;

            // Block 172.16.0.0/12 (private)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // Block 192.168.0.0/16 (private)
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            // Block 169.254.0.0/16 (link-local, includes AWS/Azure metadata at 169.254.169.254)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;

            // Block 0.0.0.0/8
            if (bytes[0] == 0)
                return true;

            // Block 100.64.0.0/10 (carrier-grade NAT, used by Tailscale)
            if (bytes[0] == 100 && (bytes[1] & 0xC0) == 64)
                return true;
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Any))
                return true;

            // Block IPv6 unique local (fc00::/7)
            if ((address.GetAddressBytes()[0] & 0xFE) == 0xFC)
                return true;

            // Block IPv6 link-local (fe80::/10)
            if (address.IsIPv6LinkLocal)
                return true;

            // Block IPv6 site-local (fec0::/10) - deprecated but still worth blocking
            if (address.IsIPv6SiteLocal)
                return true;

            // Block IPv4-mapped IPv6 addresses that map to blocked ranges
            if (address.IsIPv4MappedToIPv6)
            {
                var ipv4 = address.MapToIPv4();
                if (IsBlockedIpAddress(ipv4))
                    return true;
            }
        }

        return false;
    }
}
