using System.Text.Json;
using System.Text.Json.Nodes;
using Raytha.Application.EmailLogs;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Strips credential-bearing values from a command before it is stored on an audit row.
/// Property names are matched case-insensitively. JSON text such as widget
/// <c>SettingsJson</c> is parsed and walked the same way, and token-bearing URLs
/// inside string values are redacted.
/// </summary>
public static class AuditRequestSanitizer
{
    public const string RedactedValue = "[redacted]";

    private static readonly string[] SensitiveNameFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "api_key",
        "certificate",
        "credential",
        "privatekey",
        "private_key",
        "connectionstring",
        "connection_string",
        "smtpusername",
        "smtpuser",
        "samlresponse",
    ];

    public static bool IsSensitiveName(string name)
    {
        var lowered = name.ToLowerInvariant();
        return SensitiveNameFragments.Any(lowered.Contains);
    }

    public static string Sanitize(object message, bool redactIdentifier = false)
    {
        var node = JsonSerializer.SerializeToNode(message, message.GetType());
        if (redactIdentifier && node is JsonObject root)
        {
            foreach (var key in root.Select(p => p.Key).ToList())
            {
                if (key.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    root[key] = JsonValue.Create(RedactedValue);
                }
            }
        }

        Redact(node);
        return node?.ToJsonString() ?? "{}";
    }

    private static bool Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var changed = false;
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (IsSensitiveName(key))
                    {
                        obj[key] = JsonValue.Create(RedactedValue);
                        changed = true;
                        continue;
                    }

                    changed |= RedactValue(obj[key], rewritten => obj[key] = rewritten);
                }

                return changed;
            case JsonArray array:
                var arrayChanged = false;
                for (var i = 0; i < array.Count; i++)
                {
                    var index = i;
                    arrayChanged |= RedactValue(array[index], rewritten => array[index] = rewritten);
                }

                return arrayChanged;
            default:
                return false;
        }
    }

    private static bool RedactValue(JsonNode? node, Action<JsonNode?> replace)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            if (!TrySanitizeString(text, out var sanitized))
            {
                return false;
            }

            replace(JsonValue.Create(sanitized));
            return true;
        }

        return Redact(node);
    }

    private static bool TrySanitizeString(string text, out string sanitized)
    {
        var trimmed = text.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                var inner = JsonNode.Parse(text);
                if (inner is not null && Redact(inner))
                {
                    sanitized = inner.ToJsonString();
                    return true;
                }
            }
            catch (JsonException)
            {
                // Not JSON. Fall through and scrub the raw text.
            }
        }

        var plain = EmailBodySanitizer.Sanitize(text);
        if (!string.Equals(plain, text, StringComparison.Ordinal))
        {
            sanitized = plain;
            return true;
        }

        sanitized = text;
        return false;
    }
}
