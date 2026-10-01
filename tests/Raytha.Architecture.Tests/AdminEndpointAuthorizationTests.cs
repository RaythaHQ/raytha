using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Raytha.Web;
using Raytha.Web.Authentication;

namespace Raytha.Architecture.Tests;

/// <summary>
/// RFC-0008 §1: every endpoint under <c>/raytha</c> carries an authorization policy and none
/// carries AllowAnonymous, except the routes in <see cref="AnonymousAllowlist"/>. The table is the
/// real one: <see cref="Startup"/> registers services and maps endpoints on a WebApplication that
/// is never started, so no database is touched.
/// </summary>
[TestFixture]
public class AdminEndpointAuthorizationTests
{
    private const string RestApiPrefix = "/raytha/api/v1/";

    private sealed record AdminEndpoint(
        string Method,
        string Route,
        bool AllowsAnonymous,
        bool HasAuthorization,
        IReadOnlyList<string?> Policies
    )
    {
        public string Key => $"{Method} {Route}";
        public bool IsRestApi => Route.StartsWith(RestApiPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Dictionary<string, string> AnonymousAllowlist = new(StringComparer.Ordinal)
    {
        ["POST /raytha/api/auth/login"] = "SPA sign-in; rate limited under /raytha/api/auth",
        ["POST /raytha/api/auth/logout"] = "sign-out must work for an expired or non-admin session",
        ["GET /raytha/api/auth/schemes"] = "the login screen lists enabled sign-in schemes before anyone is signed in",
        ["POST /raytha/api/auth/magic-link"] = "magic-link sign-in request",
        ["POST /raytha/api/auth/magic-link/complete"] = "magic-link sign-in completion from the emailed token",
        ["POST /raytha/api/auth/forgot-password"] = "password reset request",
        ["GET /raytha/api/auth/forgot-password/validate"] = "password reset token check before the form renders",
        ["POST /raytha/api/auth/forgot-password/complete"] = "password reset with the emailed token",
        ["GET /raytha/api/auth/setup"] = "first-run setup status; there is no admin yet",
        ["GET /raytha/api/auth/setup/status"] = "first-run setup status; there is no admin yet",
        ["POST /raytha/api/auth/setup"] = "first-run setup; the endpoint answers 409 once InitialSetupComplete",
        ["GET /raytha/media-items/objectkey/{objectKey}"] =
            "public file redirect behind the attachment_redirect_url Liquid filter",
        ["HEAD /raytha/media-items/objectkey/{objectKey}"] =
            "same public file redirect; HEAD is what link checkers and CDNs send",
        ["GET /raytha/media-items/id/{id}"] = "public file redirect behind the attachment_redirect_url Liquid filter",
        ["HEAD /raytha/media-items/id/{id}"] =
            "same public file redirect; HEAD is what link checkers and CDNs send",
        ["GET /raytha/api/{documentName?}"] = "public Scalar reference for REST API v1 (v1.5.2 parity)",
        ["GET /raytha/api/{documentName}/swagger.json"] = "public OpenAPI document for REST API v1 (v1.5.2 parity)",
        ["GET /raytha/api/scalar.js"] = "Scalar reference asset",
        ["GET /raytha/api/scalar.aspnetcore.js"] = "Scalar reference asset",
        ["GET /raytha/api/favicon.svg"] = "Scalar reference asset",
        ["GET /raytha"] = "static SPA shell; data only flows through the authorized admin API",
        ["GET /raytha/{**path}"] = "static SPA shell for deep links; data only flows through the authorized admin API",
        ["ANY /raytha/login"] = "Razor sign-in page",
        ["ANY /raytha/login-redirect"] = "cookie LoginPath; routes an unauthenticated request to sign-in",
        ["ANY /raytha/logout"] = "Razor sign-out must work for an expired or non-admin session",
        ["ANY /raytha/login/forgot-password"] = "Razor password reset request",
        ["ANY /raytha/login/forgot-password/sent"] = "Razor password reset confirmation",
        ["ANY /raytha/login/forgot-password/complete/{token?}"] = "Razor password reset link target from email",
        ["ANY /raytha/login/magic-link"] = "Razor magic-link request",
        ["ANY /raytha/login/magic-link/complete"] = "Razor magic-link target from email",
        ["ANY /raytha/login/sso/{developerName}"] = "SSO sign-in entry point",
        ["ANY /raytha/login/jwt/{developerName}"] = "JWT sign-in callback",
        ["ANY /raytha/login/saml/{developerName}"] = "SAML sign-in callback",
        ["ANY /raytha/error/403"] = "error page re-executed by UseStatusCodePagesWithReExecute for any caller",
        ["ANY /raytha/error/404"] = "error page re-executed by UseStatusCodePagesWithReExecute for any caller",
        ["ANY /raytha/error/500"] = "error page re-executed by UseStatusCodePagesWithReExecute for any caller",
        ["ANY /raytha/functions/execute/{developerName}"] =
            "public HTTP trigger for Raytha Functions, sandboxed and refused unless the function is active (v1.5.2 parity)",
        ["ANY /raytha/themes/export/{developerName}"] =
            "theme export for import-by-URL; ExportTheme refuses unless the theme is marked exportable (v1.5.2 parity)",
    };

    private static readonly Lazy<IReadOnlyList<AdminEndpoint>> Endpoints = new(DiscoverAdminEndpoints);

    private static IReadOnlyList<AdminEndpoint> DiscoverAdminEndpoints()
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
        var startup = new Startup(builder.Configuration, builder.Environment);
        startup.ConfigureServices(builder.Services);

        using var app = builder.Build();
        startup.Configure(app, app.Environment);

        return ((IEndpointRouteBuilder)app)
            .DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => (endpoint, route: "/" + (endpoint.RoutePattern.RawText ?? string.Empty).Trim('/')))
            .Where(e => e.route.Equals("/raytha", StringComparison.OrdinalIgnoreCase)
                || e.route.StartsWith("/raytha/", StringComparison.OrdinalIgnoreCase))
            .SelectMany(e =>
            {
                var metadata = e.endpoint.Metadata;
                var authorizeData = metadata.GetOrderedMetadata<IAuthorizeData>();
                var hasAuthorization =
                    authorizeData.Count > 0
                    || metadata.GetMetadata<AuthorizationPolicy>() is not null
                    || metadata.GetMetadata<IAuthorizationRequirementData>() is not null;
                var methods = metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                return (methods is { Count: > 0 } ? methods : ["ANY"]).Select(method => new AdminEndpoint(
                    method,
                    e.route,
                    metadata.GetMetadata<IAllowAnonymous>() is not null,
                    hasAuthorization,
                    authorizeData.Select(d => d.Policy).ToList()
                ));
            })
            .ToList();
    }

    private static string? Violation(AdminEndpoint endpoint)
    {
        if (endpoint.AllowsAnonymous)
            return "carries AllowAnonymous";
        if (!endpoint.HasAuthorization)
            return "has no authorization policy";
        if (
            endpoint.IsRestApi
            && (
                endpoint.Policies.Count == 0
                || endpoint.Policies.Any(p =>
                    p is null || !p.StartsWith(RaythaApiAuthorizationHandler.POLICY_PREFIX, StringComparison.Ordinal)
                )
            )
        )
            return $"REST API v1 needs Api-prefixed policies only, has [{string.Join(", ", endpoint.Policies)}]";
        return null;
    }

    [Test]
    public void Discovery_sees_every_admin_endpoint_family()
    {
        var keys = Endpoints.Value.Select(e => e.Key).ToHashSet();

        keys.Should().HaveCountGreaterThan(200, "a broken discovery must not pass the policy check vacuously");
        keys.Should()
            .Contain(
                [
                    "GET /raytha/api/admin/users",
                    "POST /raytha/api/auth/login",
                    "POST /raytha/media-items/upload",
                    "ANY /raytha/login",
                    "GET /raytha/api/v1/Ping",
                ],
                "the admin API, auth API, media endpoints, Razor pages, and REST API v1 are all discovered"
            );
    }

    [Test]
    public void Every_admin_endpoint_requires_an_authorization_policy()
    {
        var violations = Endpoints
            .Value.Where(e => !AnonymousAllowlist.ContainsKey(e.Key))
            .Select(e => (e.Key, reason: Violation(e)))
            .Where(e => e.reason is not null)
            .Select(e => $"{e.Key} {e.reason}");

        string.Join(Environment.NewLine, violations)
            .Should()
            .BeEmpty(
                "RFC-0008 §1 requires a policy on every admin endpoint; an anonymous route belongs in AnonymousAllowlist with a reason"
            );
    }

    [Test]
    public void Anonymous_allowlist_has_no_stale_entries()
    {
        var anonymous = Endpoints.Value.Where(e => Violation(e) is not null).Select(e => e.Key).ToHashSet();

        string.Join(Environment.NewLine, AnonymousAllowlist.Keys.Where(key => !anonymous.Contains(key)))
            .Should()
            .BeEmpty("an allowlisted route that no longer exists or is now authorized must leave the allowlist");
    }
}
