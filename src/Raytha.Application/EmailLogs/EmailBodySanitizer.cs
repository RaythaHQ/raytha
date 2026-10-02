using System.Text.RegularExpressions;

namespace Raytha.Application.EmailLogs;

/// <summary>
/// Strips credential-bearing values from an email body before it is persisted
/// or returned to an admin. Reset and magic-link mail put live tokens in the
/// path; Fluid HTML-encodes '&amp;' so a query-string sanitizer that only
/// looks for '&' misses them.
/// </summary>
public static partial class EmailBodySanitizer
{
    public const string RedactedValue = "[redacted]";

    [GeneratedRegex(
        @"((?:\?|&(?:amp;|#38;|#x26;)?)(?:token|code|otp|key|secret|signature|password)=)[^&\s""'<>]*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex SensitiveQueryParameter();

    [GeneratedRegex(
        @"(/login/(?:forgot-password|magic-link)/complete/)([^/?""'\s<]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex SecretPathSegment();

    [GeneratedRegex(
        @"(Password:\s*)([^\s<]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex PasswordLabel();

    public static string Sanitize(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        var sanitized = SensitiveQueryParameter().Replace(body, $"$1{RedactedValue}");
        sanitized = SecretPathSegment().Replace(sanitized, $"$1{RedactedValue}");
        return PasswordLabel().Replace(sanitized, $"$1{RedactedValue}");
    }
}
