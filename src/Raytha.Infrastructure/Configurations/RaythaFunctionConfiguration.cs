using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Configurations;

public class RaythaFunctionConfiguration : IRaythaFunctionConfiguration
{
    public RaythaFunctionConfiguration(IConfiguration configuration)
    {
        MaxActive = Convert.ToInt32(configuration["RAYTHA_FUNCTIONS_MAX_ACTIVE"] ?? "5");
        ExecuteTimeout = TimeSpan.FromMilliseconds(Convert.ToInt32(configuration["RAYTHA_FUNCTIONS_TIMEOUT"] ?? "10000"));
        QueueTimeout = TimeSpan.FromMilliseconds(Convert.ToInt32(configuration["RAYTHA_FUNCTIONS_QUEUE_TIMEOUT"] ?? "10000"));
    }

    public bool IsEnabled => MaxActive > 0;
    public int MaxActive { get; }
    public TimeSpan ExecuteTimeout { get; }
    public TimeSpan QueueTimeout { get; }
}
