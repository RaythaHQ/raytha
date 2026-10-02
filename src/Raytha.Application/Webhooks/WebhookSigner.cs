using System.Security.Cryptography;
using System.Text;

namespace Raytha.Application.Webhooks;

/// <summary>
/// HMAC-SHA256 payload signing. The <see cref="SignatureHeader"/> header carries
/// "sha256=&lt;hex&gt;" computed over "&lt;timestamp&gt;.&lt;raw body&gt;", where the timestamp is the
/// exact <see cref="TimestampHeader"/> value, so receivers can verify authenticity and
/// integrity and reject a replayed delivery by its age.
/// </summary>
public static class WebhookSigner
{
    public const string SignatureHeader = "X-Raytha-Signature";
    public const string EventHeader = "X-Raytha-Event";
    public const string DeliveryHeader = "X-Raytha-Delivery";
    public const string TimestampHeader = "X-Raytha-Timestamp";

    public static string Sign(string timestamp, string payload, string secret)
    {
        var hash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{payload}")
        );
        return $"sha256={Convert.ToHexStringLower(hash)}";
    }

    public static bool Verify(string timestamp, string payload, string secret, string signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            return false;
        }

        var expected = Sign(timestamp, payload, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature)
        );
    }

    public static string GenerateSecret()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    }
}
