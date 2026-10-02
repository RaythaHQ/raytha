using Microsoft.Extensions.Configuration;

namespace Raytha.Web.Observability;

/// <summary>
/// Opt-in observability settings, bound from flat environment-style keys so they work
/// identically from .env, appsettings.json, or container env vars.
/// </summary>
public sealed record ObservabilityOptions
{
    public string ServiceName { get; init; } = "raytha";
    public bool EnableConsoleLogging { get; init; } = true;
    public bool EnableLokiLogging { get; init; }
    public string? LokiUrl { get; init; }
    public string? LokiUsername { get; init; }
    public string? LokiPassword { get; init; }
    public string? OtlpEndpoint { get; init; }
    public bool OtlpUseGrpc { get; init; }
    public string? SentryDsn { get; init; }
    public double SentryTracesSampleRate { get; init; }

    public bool HasOtlp => !string.IsNullOrWhiteSpace(OtlpEndpoint);
    public bool HasSentry => !string.IsNullOrWhiteSpace(SentryDsn);
    public bool HasLoki => EnableLokiLogging && !string.IsNullOrWhiteSpace(LokiUrl);

    /// <summary>True when any sink beyond the default console logger is configured.</summary>
    public bool IsEnabled => HasOtlp || HasSentry || HasLoki;

    public static ObservabilityOptions Bind(IConfiguration configuration)
    {
        return new ObservabilityOptions
        {
            ServiceName = configuration["OTEL_SERVICE_NAME"].OrDefault("raytha"),
            EnableConsoleLogging = configuration["OBSERVABILITY_LOGGING_ENABLE_CONSOLE"].AsBool(true),
            EnableLokiLogging = configuration["OBSERVABILITY_LOGGING_ENABLE_LOKI"].AsBool(false),
            LokiUrl = configuration["LOKI_URL"].NullIfEmpty(),
            LokiUsername = configuration["LOKI_USERNAME"].NullIfEmpty(),
            LokiPassword = configuration["LOKI_PASSWORD"].NullIfEmpty(),
            OtlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"].NullIfEmpty(),
            OtlpUseGrpc = string.Equals(
                configuration["OTEL_EXPORTER_OTLP_PROTOCOL"],
                "grpc",
                StringComparison.OrdinalIgnoreCase
            ),
            SentryDsn = configuration["SENTRY_DSN"].NullIfEmpty(),
            SentryTracesSampleRate = configuration["SENTRY_TRACES_SAMPLE_RATE"].AsDouble(0),
        };
    }
}

internal static class ConfigurationValueExtensions
{
    public static string OrDefault(this string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    public static string? NullIfEmpty(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool AsBool(this string? value, bool fallback) =>
        bool.TryParse(value, out var parsed) ? parsed : fallback;

    public static double AsDouble(this string? value, double fallback) =>
        double.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : fallback;
}
