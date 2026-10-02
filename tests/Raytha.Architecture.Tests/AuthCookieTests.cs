using FluentAssertions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raytha.Web;

namespace Raytha.Architecture.Tests;

/// <summary>
/// RFC-0008 §1: the session cookie as <see cref="Startup"/> configures it outside Development.
/// </summary>
[TestFixture]
public class AuthCookieTests
{
    [Test]
    public void The_auth_cookie_is_http_only_secure_and_same_site_lax_outside_development()
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                ApplicationName = Layers.Web.GetName().Name,
                EnvironmentName = Environments.Production,
                ContentRootPath = AppContext.BaseDirectory,
            }
        );
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=unused.invalid",
                ["APPLY_PENDING_MIGRATIONS"] = "false",
            }
        );
        new Startup(builder.Configuration, builder.Environment).ConfigureServices(builder.Services);
        using var app = builder.Build();

        var cookie = app
            .Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme)
            .Cookie;

        cookie.HttpOnly.Should().BeTrue();
        cookie.SecurePolicy.Should().Be(CookieSecurePolicy.Always);
        cookie.SameSite.Should().Be(SameSiteMode.Lax);
    }
}
