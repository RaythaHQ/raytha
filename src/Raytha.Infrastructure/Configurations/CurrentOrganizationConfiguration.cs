using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Infrastructure.Configurations;

public class CurrentOrganizationConfiguration : ICurrentOrganizationConfiguration
{
    private readonly IConfiguration _configuration;

    public CurrentOrganizationConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string PathBase => _configuration["PATHBASE"]!;
    public string RedirectWebsite => _configuration["REDIRECT_WEBSITE"]!;
}