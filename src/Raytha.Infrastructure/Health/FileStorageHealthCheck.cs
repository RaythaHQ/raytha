using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Health;

/// <summary>
/// Readiness probe for the configured file storage provider. Local storage verifies the
/// upload directory exists and is writable; cloud providers verify a signed URL can be
/// produced (credentials and configuration are present) without performing network I/O.
/// </summary>
public sealed class FileStorageHealthCheck : IHealthCheck
{
    public const string Name = "storage";

    private readonly IFileStorageProviderSettings _settings;
    private readonly IFileStorageProvider _provider;
    private readonly IHostEnvironment _environment;

    public FileStorageHealthCheck(
        IFileStorageProviderSettings settings,
        IFileStorageProvider provider,
        IHostEnvironment environment
    )
    {
        _settings = settings;
        _provider = provider;
        _environment = environment;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var data = new Dictionary<string, object> { ["provider"] = _provider.GetName() };

        try
        {
            if (_settings.UseLocal)
            {
                var directory = Path.Combine(
                    _environment.ContentRootPath,
                    _settings.LocalDirectory
                );
                data["directory"] = directory;

                if (!Directory.Exists(directory))
                {
                    return HealthCheckResult.Unhealthy(
                        "Local storage directory does not exist.",
                        data: data
                    );
                }

                var probe = Path.Combine(directory, $".healthz-{Guid.NewGuid():N}");
                await File.WriteAllTextAsync(probe, "ok", cancellationToken);
                File.Delete(probe);

                return HealthCheckResult.Healthy("Local storage directory is writable.", data);
            }

            await _provider.GetDownloadUrlAsync(
                "healthz-probe",
                DateTime.UtcNow.AddMinutes(1),
                inline: true
            );
            return HealthCheckResult.Healthy("Storage provider is configured.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Storage probe failed.", ex, data);
        }
    }
}
