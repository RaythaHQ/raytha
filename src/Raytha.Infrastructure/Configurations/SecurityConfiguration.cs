using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Configurations;

/// <summary>
/// Provides security-related configuration settings.
/// </summary>
public class SecurityConfiguration : ISecurityConfiguration
{
    private readonly IConfiguration _configuration;

    public SecurityConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public bool AllowInternalUrlImports =>
        Convert.ToBoolean(_configuration["ALLOW_INTERNAL_URL_IMPORTS"] ?? "false");
}

