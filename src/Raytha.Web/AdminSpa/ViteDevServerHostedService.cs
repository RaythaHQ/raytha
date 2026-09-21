using System.Diagnostics;
using System.Net.Sockets;

namespace Raytha.Web.AdminSpa;

/// <summary>
/// Starts the admin Vite dev server alongside Raytha.Web in Development so
/// <c>dotnet run --project src/Raytha.Web</c> is enough to get <c>/raytha</c> with HMR.
/// Reuses a listener on <c>AdminSpa:DevServerUrl</c> only when GET <c>/raytha/</c> returns 200.
/// </summary>
public sealed class ViteDevServerHostedService : IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ViteDevServerHostedService> _logger;
    private readonly AdminSpaProxyState _proxyState;
    private Process? _process;

    public ViteDevServerHostedService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<ViteDevServerHostedService> logger,
        AdminSpaProxyState proxyState
    )
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _proxyState = proxyState;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        var url = AdminSpaExtensions.DevServerUrl(_configuration);
        if (await IsTcpOpenAsync(url, cancellationToken))
        {
            if (await IsAdminSpaAsync(url, cancellationToken))
            {
                _logger.LogInformation("Admin Vite already running at {Url}", url);
                _proxyState.ViteReady = true;
                return;
            }

            _logger.LogWarning(
                "Something is listening at {Url} but GET /raytha/ is not 200. "
                    + "Raytha will not proxy /raytha there (a leftover Vite from another checkout causes a 404). "
                    + "Stop that process or set AdminSpa:DevServerUrl. Serving wwwroot/raytha until then.",
                url
            );
            return;
        }

        if (!_configuration.GetValue("AdminSpa:AutoStart", defaultValue: true))
        {
            return;
        }

        var adminDir = ResolveAdminDirectory(_environment.ContentRootPath);
        if (adminDir is null)
        {
            _logger.LogWarning(
                "Could not locate src/admin relative to {ContentRoot}. Start it manually: cd src/admin && pnpm dev",
                _environment.ContentRootPath
            );
            return;
        }

        if (!Directory.Exists(Path.Combine(adminDir, "node_modules")))
        {
            _logger.LogInformation("Installing admin workspace packages in {AdminDir}…", adminDir);
            var install = await RunAsync("pnpm", "install", adminDir, cancellationToken);
            if (install != 0)
            {
                _logger.LogWarning(
                    "pnpm install failed (exit {Code}). /raytha will use the published bundle until Vite is started manually.",
                    install
                );
                return;
            }
        }

        _logger.LogInformation("Starting admin Vite at {Url} from {AdminDir}", url, adminDir);
        _process = StartProcess(
            "pnpm",
            "dev",
            adminDir,
            new Dictionary<string, string> { ["RAYTHA_DEV_PROXY"] = "1" }
        );

        if (_process is null)
        {
            _logger.LogWarning(
                "Could not start pnpm. Is pnpm on PATH? /raytha will use the published bundle until Vite is started manually."
            );
            return;
        }

        _ = ForwardStreamAsync(_process.StandardOutput, LogLevel.Debug, cancellationToken);
        _ = ForwardStreamAsync(_process.StandardError, LogLevel.Information, cancellationToken);

        var ready = await WaitUntilAdminSpaAsync(url, TimeSpan.FromSeconds(60), cancellationToken);
        if (!ready)
        {
            _logger.LogWarning(
                "Admin Vite did not become ready at {Url} within 60s. /raytha will use the published bundle until it is.",
                url
            );
            return;
        }

        _proxyState.ViteReady = true;
        _logger.LogInformation("Admin Vite is ready at {Url}", url);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Leave Vite running across `dotnet watch` restarts so the next host iteration reuses it
        // instead of paying another cold start. Kill the process on :5203 for a clean slate.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _process?.Dispose();
        _process = null;
    }

    private async Task ForwardStreamAsync(StreamReader reader, LogLevel level, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    _logger.Log(level, "[vite] {Line}", line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // host shutting down
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Vite log stream ended");
        }
    }

    /// <summary>Raytha.Web's ContentRoot is <c>src/Raytha.Web</c>; the admin workspace is <c>src/admin</c>.</summary>
    private static string? ResolveAdminDirectory(string contentRoot)
    {
        foreach (var relative in new[] { Path.Combine("..", "admin"), Path.Combine("..", "..", "admin") })
        {
            var candidate = Path.GetFullPath(Path.Combine(contentRoot, relative));
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "package.json")))
            {
                return candidate;
            }
        }
        return null;
    }

    private static Process? StartProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        try
        {
            return Process.Start(startInfo);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<int> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken
    )
    {
        using var process = StartProcess(fileName, arguments, workingDirectory);
        if (process is null)
        {
            return -1;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static async Task<bool> WaitUntilAdminSpaAsync(string url, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsAdminSpaAsync(url, cancellationToken))
            {
                return true;
            }

            await Task.Delay(250, cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsTcpOpenAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(uri.Host, uri.Port, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// True only when Vite is actually serving the admin shell. A TCP listener on the port is not
    /// enough — a stale or foreign process 404s <c>/raytha/</c>.
    /// </summary>
    internal static async Task<bool> IsAdminSpaAsync(string url, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url.TrimEnd('/') + AdminSpaExtensions.BasePath + "/", UriKind.Absolute, out var probe))
        {
            return false;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await client.GetAsync(probe, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
