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

            // Block 0.0.0.0
            if (bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 0)
                return true;
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
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
