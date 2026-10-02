using System.Net;
using CSharpVitamins;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Raytha.Application.AuthenticationSchemes;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;
using Raytha.Web.Services;
using RaythaZero.Web.Services;

namespace Raytha.Architecture.Tests;

/// <summary>
/// Links that leave the request (emails, SSO callbacks, API media URLs) must not take their host
/// from the request, which a client or an untrusted proxy controls.
/// </summary>
[TestFixture]
public class SiteUrlTests
{
    [TestCase("https://cms.example.com", "", "https://cms.example.com")]
    [TestCase("https://cms.example.com/", "", "https://cms.example.com")]
    [TestCase(" https://cms.example.com/ ", "/site", "https://cms.example.com")]
    [TestCase("https://cms.example.com/site", "/site", "https://cms.example.com")]
    [TestCase("https://cms.example.com/site/", "/site", "https://cms.example.com")]
    [TestCase("https://cms.example.com/SITE", "/site", "https://cms.example.com")]
    [TestCase("https://cms.example.com/cms", "", "https://cms.example.com/cms")]
    [TestCase("https://cms.example.com/cms", "/site", "https://cms.example.com/cms")]
    [TestCase("https://site", "/site", "https://site")]
    [TestCase("https://cms.example.com/mysite", "/site", "https://cms.example.com/mysite")]
    public void The_site_root_is_website_url_without_a_trailing_slash_or_the_path_base(
        string websiteUrl,
        string pathBase,
        string expected
    )
    {
        RelativeUrlBuilder.SiteRoot(websiteUrl, pathBase, "http://evil.example").Should().Be(expected);
    }

    [TestCase(null, "http://localhost:5200", "http://localhost:5200")]
    [TestCase("  ", "http://localhost:5200", "http://localhost:5200")]
    [TestCase("", null, "")]
    public void Without_a_website_url_the_site_root_is_the_development_origin_or_nothing(
        string? websiteUrl,
        string? developmentOrigin,
        string expected
    )
    {
        RelativeUrlBuilder.SiteRoot(websiteUrl, "/site", developmentOrigin).Should().Be(expected);
    }

    [TestCase("Production")]
    [TestCase("Development")]
    public void Website_url_wins_over_a_forged_request_host(string environment)
    {
        var urls = Builder(environment, websiteUrl: "https://cms.example.com/site", pathBase: "/site");

        urls.GetBaseUrl().Should().Be("https://cms.example.com/site");
        urls.GetSiteRoot().Should().Be("https://cms.example.com");
        urls.UserLoginUrl().Should().Be("https://cms.example.com/site/account/login");
        urls.AdminLoginUrl("/raytha/users")
            .Should()
            .Be("https://cms.example.com/site/raytha/login?returnUrl=%2Fraytha%2Fusers");
    }

    [Test]
    public void Before_setup_outside_development_links_are_relative_to_the_path_base()
    {
        var urls = Builder("Production", websiteUrl: "", pathBase: "/site");

        urls.GetBaseUrl().Should().Be("/site");
        urls.GetSiteRoot().Should().BeEmpty();
    }

    [Test]
    public void Before_setup_in_development_links_use_the_request_origin()
    {
        var urls = Builder("Development", websiteUrl: "", pathBase: "/site");

        urls.GetBaseUrl().Should().Be("https://evil.example/site");
    }

    [Test]
    public void Password_reset_links_escape_the_token()
    {
        var urls = Builder("Production", websiteUrl: "https://cms.example.com", pathBase: "");

        urls.UserForgotPasswordCompleteUrl("a b/c+d")
            .Should()
            .Be("https://cms.example.com/account/login/forgot-password/complete/a%20b%2Fc%2Bd");
        urls.AdminForgotPasswordCompleteUrl("t/1")
            .Should()
            .Be("https://cms.example.com/raytha/login/forgot-password/complete/t%2F1");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void The_media_redirect_url_carries_the_path_base_once(bool inRequest)
    {
        var urls = Builder(
            "Production",
            websiteUrl: "https://cms.example.com/site",
            pathBase: "/site",
            inRequest
        );

        urls.MediaRedirectToFileUrl("k.png")
            .Should()
            .Be("https://cms.example.com/site/raytha/media-items/objectkey/k.png");
        urls.MediaRedirectToFilePath("k.png")
            .Should()
            .Be("/site/raytha/media-items/objectkey/k.png");
        urls.MediaFileLocalStorageUrl("k.png").Should().Be("https://cms.example.com/site/_static-files/k.png");
    }

    [Test]
    public void The_public_sso_callback_is_under_account_and_built_from_website_url()
    {
        var urls = Builder("Production", websiteUrl: "https://cms.example.com", pathBase: "");

        var loginUrl = urls.GetSingleSignOnCallbackJwtUrl("Public", "okta", "https://idp.example/login", "/members");

        loginUrl.Should()
            .Be(
                "https://idp.example/login?raytha_callback_url="
                    + Uri.EscapeDataString("https://cms.example.com/account/login/jwt/okta?returnUrl=%2Fmembers")
            );
    }

    [Test]
    public void The_client_ip_is_the_connection_peer_and_never_a_header()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.9");
        context.Request.Headers["CF-Connecting-IP"] = "6.6.6.6";
        context.Request.Headers["X-Forwarded-For"] = "7.7.7.7";

        var user = new CurrentUser(new HttpContextAccessor { HttpContext = context });

        user.RemoteIpAddress.Should().Be("203.0.113.9");
    }

    [Test]
    public void An_ipv6_client_ip_is_kept_as_is()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::7");

        new CurrentUser(new HttpContextAccessor { HttpContext = context })
            .RemoteIpAddress.Should()
            .Be("2001:db8::7");
    }

    internal static RelativeUrlBuilder Builder(
        string environment,
        string websiteUrl,
        string pathBase,
        bool inRequest = true
    )
    {
        var accessor = new HttpContextAccessor();
        if (inRequest)
        {
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("evil.example");
            context.Request.PathBase = pathBase;
            context.Request.Headers["X-Forwarded-Host"] = "evil.example";
            accessor.HttpContext = context;
        }
        return new RelativeUrlBuilder(
            accessor,
            new MediaRouteLinks(),
            new FakeOrganization(websiteUrl, pathBase),
            new HostingEnvironment { EnvironmentName = environment }
        );
    }

    /// <summary>The one named route the builder asks for, prefixed by the request's path base as routing does.</summary>
    private sealed class MediaRouteLinks : LinkGenerator
    {
        private static string Path(object? values) =>
            $"/raytha/media-items/objectkey/{new RouteValueDictionary(values)["objectKey"]}";

        public override string? GetPathByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            RouteValueDictionary values,
            RouteValueDictionary? ambientValues = null,
            PathString? pathBase = null,
            FragmentString fragment = default,
            LinkOptions? options = null
        ) => (pathBase ?? httpContext.Request.PathBase) + Path(values);

        public override string? GetPathByAddress<TAddress>(
            TAddress address,
            RouteValueDictionary values,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions? options = null
        ) => pathBase + Path(values);

        public override string? GetUriByAddress<TAddress>(
            HttpContext httpContext,
            TAddress address,
            RouteValueDictionary values,
            RouteValueDictionary? ambientValues = null,
            string? scheme = null,
            HostString? host = null,
            PathString? pathBase = null,
            FragmentString fragment = default,
            LinkOptions? options = null
        ) => throw new NotSupportedException();

        public override string? GetUriByAddress<TAddress>(
            TAddress address,
            RouteValueDictionary values,
            string scheme,
            HostString host,
            PathString pathBase = default,
            FragmentString fragment = default,
            LinkOptions? options = null
        ) => throw new NotSupportedException();
    }

    private sealed class FakeOrganization(string websiteUrl, string pathBase) : ICurrentOrganization
    {
        public bool InitialSetupComplete => websiteUrl.Length > 0;
        public string OrganizationName => "Test";
        public string WebsiteUrl => websiteUrl;
        public string TimeZone => "Etc/UTC";
        public string DateFormat => "yyyy-MM-dd";
        public string SmtpDefaultFromAddress => "";
        public string SmtpDefaultFromName => "";
        public bool EmailAndPasswordIsEnabledForAdmins => true;
        public bool EmailAndPasswordIsEnabledForUsers => true;
        public ShortGuid? HomePageId => null;
        public string HomePageType => "";
        public ShortGuid ActiveThemeId => ShortGuid.Empty;
        public string PathBase => pathBase;
        public string RedirectWebsite => "";
        public OrganizationTimeZoneConverter TimeZoneConverter => throw new NotSupportedException();
        public IEnumerable<AuthenticationSchemeDto> AuthenticationSchemes => [];
        public IEnumerable<ContentTypeDto> ContentTypes => [];
    }
}
