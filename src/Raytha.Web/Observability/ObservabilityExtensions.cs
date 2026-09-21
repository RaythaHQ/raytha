using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Sinks.OpenTelemetry;

namespace Raytha.Web.Observability;

/// <summary>
/// Opt-in observability. Nothing here changes behaviour unless an env var enables it:
/// <list type="bullet">
/// <item><c>OTEL_EXPORTER_OTLP_ENDPOINT</c> turns on OpenTelemetry traces + metrics (and an OTLP log sink).</item>
/// <item><c>LOKI_URL</c> + <c>OBSERVABILITY_LOGGING_ENABLE_LOKI=true</c> ships logs to Loki / VictoriaLogs.</item>
/// <item><c>SENTRY_DSN</c> enables Sentry error reporting.</item>
/// </list>
/// When any sink is configured Serilog replaces the default logger; otherwise the stock
/// Microsoft console logging stays untouched.
/// </summary>
public static class ObservabilityExtensions
{
    public static IHostBuilder UseRaythaObservability(this IHostBuilder hostBuilder)
    {
        return hostBuilder
            .ConfigureServices(
                (context, services) =>
                {
                    var options = ObservabilityOptions.Bind(context.Configuration);
                    services.AddSingleton(options);

                    if (!options.HasOtlp)
                    {
                        return;
                    }

                    var otel = services.AddOpenTelemetry();

                    otel.ConfigureResource(resource =>
                        resource.AddService(
                            serviceName: options.ServiceName,
                            serviceVersion: ResolveVersion(),
                            serviceInstanceId: Environment.MachineName
                        )
                    );

                    otel.WithTracing(tracing =>
                        tracing
                            .AddAspNetCoreInstrumentation(o =>
                            {
                                o.RecordException = true;
                                o.Filter = ctx =>
                                    !ctx.Request.Path.StartsWithSegments("/healthz");
                            })
                            .AddHttpClientInstrumentation()
                    );

                    otel.WithMetrics(metrics =>
                        metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
                    );

                    otel.UseOtlpExporter(
                        options.OtlpUseGrpc
                            ? OtlpExportProtocol.Grpc
                            : OtlpExportProtocol.HttpProtobuf,
                        new Uri(options.OtlpEndpoint!)
                    );
                }
            )
            .UseSerilogWhenConfigured();
    }

    /// <summary>Sentry hooks into the web host pipeline, so it is wired on the IWebHostBuilder.</summary>
    public static IWebHostBuilder UseRaythaSentry(this IWebHostBuilder webBuilder)
    {
        var options = ObservabilityOptions.Bind(BuildBootstrapConfiguration());
        if (!options.HasSentry)
        {
            return webBuilder;
        }

        return webBuilder.UseSentry(sentry =>
        {
            sentry.Dsn = options.SentryDsn;
            sentry.Release = ResolveVersion();
            sentry.TracesSampleRate = options.SentryTracesSampleRate;
            sentry.SendDefaultPii = false;
        });
    }

    private static IHostBuilder UseSerilogWhenConfigured(this IHostBuilder hostBuilder)
    {
        var options = ObservabilityOptions.Bind(BuildBootstrapConfiguration());
        if (!options.IsEnabled)
        {
            return hostBuilder;
        }

        return hostBuilder.UseSerilog(
            (context, services, loggerConfiguration) =>
            {
                var bound = ObservabilityOptions.Bind(context.Configuration);

                loggerConfiguration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("service", bound.ServiceName)
                    .Enrich.WithProperty("version", ResolveVersion())
                    .Enrich.WithProperty("environment", context.HostingEnvironment.EnvironmentName);

                if (bound.EnableConsoleLogging)
                {
                    loggerConfiguration.WriteTo.Console();
                }

                if (bound.HasLoki)
                {
                    loggerConfiguration.WriteTo.GrafanaLoki(
                        bound.LokiUrl!.TrimEnd('/'),
                        labels: new[]
                        {
                            new LokiLabel { Key = "app", Value = bound.ServiceName },
                            new LokiLabel
                            {
                                Key = "env",
                                Value = context.HostingEnvironment.EnvironmentName,
                            },
                        },
                        credentials: string.IsNullOrEmpty(bound.LokiUsername)
                            ? null
                            : new LokiCredentials
                            {
                                Login = bound.LokiUsername,
                                Password = bound.LokiPassword ?? string.Empty,
                            }
                    );
                }

                if (bound.HasOtlp)
                {
                    loggerConfiguration.WriteTo.OpenTelemetry(otel =>
                    {
                        otel.Endpoint = bound.OtlpEndpoint;
                        otel.Protocol = bound.OtlpUseGrpc
                            ? OtlpProtocol.Grpc
                            : OtlpProtocol.HttpProtobuf;
                        otel.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = bound.ServiceName,
                            ["service.version"] = ResolveVersion(),
                            ["deployment.environment"] =
                                context.HostingEnvironment.EnvironmentName,
                        };
                    });
                }
            }
        );
    }

    /// <summary>
    /// The host's configuration is not available until it is built, but Serilog and Sentry
    /// must be decided before that. Mirror the default host's env-var + .env sources here.
    /// </summary>
    private static IConfiguration BuildBootstrapConfiguration()
    {
        return new ConfigurationBuilder().AddEnvironmentVariables().Build();
    }

    private static string ResolveVersion()
    {
        return Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion?.Split('+')[0]
            ?? "0.0.0";
    }
}
