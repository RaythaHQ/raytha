namespace Raytha.Infrastructure.Health;

public static class HealthCheckTags
{
    /// <summary>Checks that gate /healthz/ready. Liveness (/healthz) runs none.</summary>
    public const string Ready = "ready";
}
