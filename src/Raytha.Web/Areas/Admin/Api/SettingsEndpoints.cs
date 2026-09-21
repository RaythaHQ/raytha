using CSharpVitamins;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.AuditLogs.Commands;
using Raytha.Application.AuditLogs.Queries;
using Raytha.Application.AuthenticationSchemes.Commands;
using Raytha.Application.AuthenticationSchemes.Queries;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.EmailLogs.Commands;
using Raytha.Application.EmailLogs.Queries;
using Raytha.Application.FeatureFlags.Commands;
using Raytha.Application.FeatureFlags.Queries;
using Raytha.Application.Login.Commands;
using Raytha.Application.Maintenance.Queries;
using Raytha.Application.OrganizationSettings.Commands;
using Raytha.Application.OrganizationSettings.Queries;
using Raytha.Application.Webhooks.Commands;
using Raytha.Application.Webhooks.Queries;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// System settings: configuration, SMTP, authentication schemes, audit logs, email log,
/// webhooks, feature flags, maintenance, background tasks, profile and version.
/// </summary>
public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettings(this RouteGroupBuilder admin)
    {
        // ---- Anything signed-in admin can hit ----
        admin.MapGet("/version", Version).WithTags("Admin platform");
        admin.MapPut("/profile", ChangeProfileHandler).WithTags("Admin profile");
        admin.MapGet("/background-tasks/{id}", BackgroundTask).WithTags("Admin background tasks");

        // ---- system_settings ----
        var system = admin.MapGroup("")
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);

        var config = system.MapGroup("/configuration").WithTags("Admin configuration");
        config.MapGet("", GetConfiguration);
        config.MapPut("", EditConfigurationHandler);
        config.MapGet("/options", ConfigurationOptions);

        var smtp = system.MapGroup("/smtp").WithTags("Admin SMTP");
        smtp.MapGet("", GetSmtp);
        smtp.MapPut("", EditSmtpHandler);

        var schemes = system.MapGroup("/authentication-schemes").WithTags("Admin authentication schemes");
        schemes.MapGet("", ListSchemes);
        schemes.MapGet("/{id}", GetScheme);
        schemes.MapPost("", CreateScheme);
        schemes.MapPut("/{id}", EditScheme);
        schemes.MapDelete("/{id}", DeleteScheme);

        var emailLog = system.MapGroup("/email-log").WithTags("Admin email log");
        emailLog.MapGet("", ListEmailLogs);
        emailLog.MapGet("/{id}", GetEmailLog);
        emailLog.MapDelete("", ClearEmailLogs);

        var webhooks = system.MapGroup("/webhooks").WithTags("Admin webhooks");
        webhooks.MapGet("", ListWebhooks);
        webhooks.MapGet("/events", WebhookEvents);
        webhooks.MapGet("/deliveries", ListDeliveries);
        webhooks.MapDelete("/deliveries", ClearDeliveries);
        webhooks.MapPost("/deliveries/{id}/redeliver", Redeliver);
        webhooks.MapGet("/{id}", GetWebhook);
        webhooks.MapPost("", CreateWebhookHandler);
        webhooks.MapPut("/{id}", EditWebhookHandler);
        webhooks.MapDelete("/{id}", DeleteWebhookHandler);
        webhooks.MapPost("/{id}/test", TestWebhookHandler);

        var flags = system.MapGroup("/feature-flags").WithTags("Admin feature flags");
        flags.MapGet("", ListFlags);
        flags.MapPut("/{key}", SetFlag);

        system.MapGet("/maintenance", Maintenance).WithTags("Admin maintenance");

        // ---- audit_logs ----
        var audit = admin.MapGroup("/audit-logs").WithTags("Admin audit logs");
        audit.MapGet("", ListAuditLogs).RequireAuthorization(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION);
        audit.MapGet("/categories", AuditLogCategories)
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION);
        audit.MapDelete("", ClearAuditLogs)
            .RequireAuthorization(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);

        return admin;
    }

    // ---- platform / profile / background tasks ----

    private static IResult Version(ICurrentVersion version, IWebHostEnvironment env) =>
        Results.Ok(new { version = version.Version, environment = env.EnvironmentName });

    public sealed record ChangeProfileRequest(string FirstName, string LastName);

    private static async Task<IResult> ChangeProfileHandler(
        [FromBody] ChangeProfileRequest body,
        ICurrentUser currentUser,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new ChangeProfile.Command
                {
                    Id = currentUser.UserId!.Value,
                    FirstName = body.FirstName,
                    LastName = body.LastName,
                }
            )
        );

    private static async Task<IResult> BackgroundTask(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetBackgroundTaskById.Query { Id = id }));

    // ---- configuration ----

    private static async Task<IResult> GetConfiguration(ISender mediator) =>
        AdminResults.From(
            await mediator.Send(new GetOrganizationSettings.Query()),
            s => new
            {
                organizationName = s.OrganizationName,
                websiteUrl = s.WebsiteUrl,
                timeZone = s.TimeZone,
                dateFormat = s.DateFormat,
                smtpDefaultFromAddress = s.SmtpDefaultFromAddress,
                smtpDefaultFromName = s.SmtpDefaultFromName,
                homePageId = s.HomePageId?.ToString(),
                homePageType = s.HomePageType,
                activeThemeId = s.ActiveThemeId.ToString(),
            }
        );

    private static async Task<IResult> EditConfigurationHandler(
        [FromBody] EditConfiguration.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body));

    private static IResult ConfigurationOptions()
    {
        return Results.Ok(
            new
            {
                timeZones = DateTimeExtensions
                    .GetTimeZoneDisplayNames()
                    .Select(kv => new { value = kv.Key, label = kv.Value }),
                dateFormats = DateTimeExtensions
                    .GetDateFormats()
                    .Select(f => new { value = f, label = DateTime.UtcNow.ToString(f) }),
            }
        );
    }

    // ---- SMTP ----

    private static async Task<IResult> GetSmtp(ISender mediator, IEmailerConfiguration emailer)
    {
        var missingEnv = emailer.IsMissingSmtpEnvVars();
        return AdminResults.From(
            await mediator.Send(new GetOrganizationSettings.Query()),
            s => new
            {
                smtpOverrideSystem = missingEnv || s.SmtpOverrideSystem,
                smtpHost = s.SmtpHost,
                smtpPort = s.SmtpPort,
                smtpUsername = s.SmtpUsername,
                // Never echo the stored password; the client sends it back only when changing it.
                hasSmtpPassword = !string.IsNullOrEmpty(s.SmtpPassword),
                missingSmtpEnvironmentVariables = missingEnv,
            }
        );
    }

    /// <summary>Request DTO because <see cref="EditSmtp.Command"/> hides its fields from JSON (audit log redaction).</summary>
    public sealed record EditSmtpRequest(
        bool SmtpOverrideSystem,
        string? SmtpHost,
        int? SmtpPort,
        string? SmtpUsername,
        string? SmtpPassword
    );

    private static async Task<IResult> EditSmtpHandler([FromBody] EditSmtpRequest body, ISender mediator) =>
        AdminResults.FromId(
            await mediator.Send(
                new EditSmtp.Command
                {
                    SmtpOverrideSystem = body.SmtpOverrideSystem,
                    SmtpHost = body.SmtpHost ?? string.Empty,
                    SmtpPort = body.SmtpPort,
                    SmtpUsername = body.SmtpUsername ?? string.Empty,
                    SmtpPassword = body.SmtpPassword ?? string.Empty,
                }
            )
        );

    // ---- authentication schemes ----

    private static async Task<IResult> ListSchemes(
        [AsParameters] PagedQuery paging,
        [FromQuery] bool? isEnabledForAdmins,
        [FromQuery] bool? isEnabledForUsers,
        ISender mediator
    )
    {
        var query = new GetAuthenticationSchemes.Query
        {
            IsEnabledForAdmins = isEnabledForAdmins,
            IsEnabledForUsers = isEnabledForUsers,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetScheme(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetAuthenticationSchemeById.Query { Id = id }));

    /// <summary>Request DTOs because the commands mark secrets <c>[JsonIgnore]</c> for audit-log redaction.</summary>
    public sealed record AuthenticationSchemeRequest(
        string Label,
        string? DeveloperName,
        string AuthenticationSchemeType,
        string? LoginButtonText,
        string? SignInUrl,
        string? SignOutUrl,
        bool IsEnabledForUsers,
        bool IsEnabledForAdmins,
        string? JwtSecretKey,
        bool JwtUseHighSecurity,
        string? SamlCertificate,
        string? SamlIdpEntityId,
        int MagicLinkExpiresInSeconds,
        int BruteForceProtectionMaxFailedAttempts,
        int BruteForceProtectionWindowInSeconds
    );

    private static async Task<IResult> CreateScheme(
        [FromBody] AuthenticationSchemeRequest body,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new CreateAuthenticationScheme.Command
                {
                    Label = body.Label,
                    DeveloperName = body.DeveloperName ?? string.Empty,
                    AuthenticationSchemeType = body.AuthenticationSchemeType,
                    LoginButtonText = body.LoginButtonText ?? string.Empty,
                    SignInUrl = body.SignInUrl ?? string.Empty,
                    SignOutUrl = body.SignOutUrl ?? string.Empty,
                    IsEnabledForUsers = body.IsEnabledForUsers,
                    IsEnabledForAdmins = body.IsEnabledForAdmins,
                    JwtSecretKey = body.JwtSecretKey ?? string.Empty,
                    JwtUseHighSecurity = body.JwtUseHighSecurity,
                    SamlCertificate = body.SamlCertificate ?? string.Empty,
                    SamlIdpEntityId = body.SamlIdpEntityId ?? string.Empty,
                }
            ),
            created: true
        );

    private static async Task<IResult> EditScheme(
        string id,
        [FromBody] AuthenticationSchemeRequest body,
        ISender mediator
    ) =>
        AdminResults.FromId(
            await mediator.Send(
                new EditAuthenticationScheme.Command
                {
                    Id = id,
                    Label = body.Label,
                    AuthenticationSchemeType = body.AuthenticationSchemeType,
                    LoginButtonText = body.LoginButtonText ?? string.Empty,
                    SignInUrl = body.SignInUrl ?? string.Empty,
                    SignOutUrl = body.SignOutUrl ?? string.Empty,
                    IsEnabledForUsers = body.IsEnabledForUsers,
                    IsEnabledForAdmins = body.IsEnabledForAdmins,
                    JwtSecretKey = body.JwtSecretKey ?? string.Empty,
                    JwtUseHighSecurity = body.JwtUseHighSecurity,
                    SamlCertificate = body.SamlCertificate ?? string.Empty,
                    SamlIdpEntityId = body.SamlIdpEntityId ?? string.Empty,
                    MagicLinkExpiresInSeconds = body.MagicLinkExpiresInSeconds,
                    BruteForceProtectionMaxFailedAttempts = body.BruteForceProtectionMaxFailedAttempts,
                    BruteForceProtectionWindowInSeconds = body.BruteForceProtectionWindowInSeconds,
                }
            )
        );

    private static async Task<IResult> DeleteScheme(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteAuthenticationScheme.Command { Id = id }));

    // ---- audit logs ----

    private static async Task<IResult> ListAuditLogs(
        [AsParameters] PagedQuery paging,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? category,
        [FromQuery] string? entityId,
        [FromQuery] string? userEmail,
        ISender mediator
    )
    {
        ShortGuid? entity = null;
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            if (!ShortGuid.TryParse(entityId.Trim(), out ShortGuid parsed))
            {
                return AdminResults.Problem($"{entityId} is not a valid unique entity ID.");
            }
            entity = parsed;
        }

        var query = new GetAuditLogs.Query
        {
            StartDateAsUtc = startDate,
            EndDateAsUtc = endDate,
            Category = category ?? string.Empty,
            EntityId = entity,
            EmailAddress = userEmail?.Trim() ?? string.Empty,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static readonly Lazy<string[]> LogCategories = new(() =>
        typeof(ILoggableRequest)
            .Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ILoggableRequest).IsAssignableFrom(t))
            .Select(t =>
                // Mirrors LoggableRequest<T>.GetLogName() without instantiating commands that have `required` members.
                (t.FullName ?? t.Name)
                    .Replace("Raytha.Application.", string.Empty)
                    .Replace("+Command", string.Empty)
                    .Replace("NavigationMenu", "Menu")
                    .Replace("NavigationMenuItem", "MenuItem")
                    .Replace("RaythaFunction", "Function")
            )
            .Distinct()
            .OrderBy(n => n)
            .ToArray()
    );

    private static IResult AuditLogCategories() => Results.Ok(LogCategories.Value);

    private static async Task<IResult> ClearAuditLogs(ISender mediator) =>
        AdminResults.From(await mediator.Send(new ClearAllAuditLogs.Command()));

    // ---- email log ----

    private static async Task<IResult> ListEmailLogs(
        [AsParameters] PagedQuery paging,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? toAddress,
        [FromQuery] bool? isSuccess,
        ISender mediator
    )
    {
        var query = new GetEmailLogs.Query
        {
            StartDateAsUtc = startDate,
            EndDateAsUtc = endDate,
            ToAddress = toAddress,
            IsSuccess = isSuccess,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> GetEmailLog(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetEmailLogById.Query { Id = id }));

    private static async Task<IResult> ClearEmailLogs(ISender mediator) =>
        AdminResults.From(await mediator.Send(new ClearEmailLog.Command()));

    // ---- webhooks ----

    private static async Task<IResult> ListWebhooks(
        [AsParameters] PagedQuery paging,
        [FromQuery] bool? isActive,
        ISender mediator
    )
    {
        var query = new GetWebhooks.Query
        {
            IsActive = isActive,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> WebhookEvents(ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWebhookEvents.Query()));

    private static async Task<IResult> ListDeliveries(
        [AsParameters] PagedQuery paging,
        [FromQuery] string? webhookId,
        [FromQuery] string? eventName,
        [FromQuery] string? status,
        ISender mediator
    )
    {
        var query = new GetWebhookDeliveries.Query
        {
            WebhookId = string.IsNullOrWhiteSpace(webhookId) ? null : webhookId,
            EventName = eventName,
            Status = status,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize,
            Search = paging.Search,
        };
        if (paging.HasOrderBy)
        {
            query = query with { OrderBy = paging.OrderBy! };
        }
        return AdminResults.Paged(await mediator.Send(query), paging);
    }

    private static async Task<IResult> ClearDeliveries(ISender mediator) =>
        AdminResults.From(await mediator.Send(new ClearWebhookDeliveries.Command()));

    private static async Task<IResult> Redeliver(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new RedeliverWebhookDelivery.Command { Id = id }));

    private static async Task<IResult> GetWebhook(string id, ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetWebhookById.Query { Id = id }));

    /// <summary>Returns <c>{ id, secret }</c>; the secret is only shown once.</summary>
    private static async Task<IResult> CreateWebhookHandler([FromBody] CreateWebhook.Command body, ISender mediator)
    {
        var response = await mediator.Send(body);
        return response.Success
            ? Results.Created((string?)null, new { id = response.Result.Id.ToString(), secret = response.Result.Secret })
            : AdminResults.Problem(response.GetErrors());
    }

    private static async Task<IResult> EditWebhookHandler(
        string id,
        [FromBody] EditWebhook.Command body,
        ISender mediator
    ) => AdminResults.FromId(await mediator.Send(body with { Id = id }));

    private static async Task<IResult> DeleteWebhookHandler(string id, ISender mediator) =>
        AdminResults.NoContent(await mediator.Send(new DeleteWebhook.Command { Id = id }));

    private static async Task<IResult> TestWebhookHandler(string id, ISender mediator) =>
        AdminResults.FromId(await mediator.Send(new TestWebhook.Command { Id = id }));

    // ---- feature flags ----

    private static async Task<IResult> ListFlags(ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetFeatureFlags.Query()));

    public sealed record SetFlagRequest(bool IsEnabled);

    private static async Task<IResult> SetFlag(string key, [FromBody] SetFlagRequest body, ISender mediator) =>
        AdminResults.From(await mediator.Send(new SetFeatureFlag.Command { Key = key, IsEnabled = body.IsEnabled }));

    // ---- maintenance ----

    private static async Task<IResult> Maintenance(ISender mediator) =>
        AdminResults.From(await mediator.Send(new GetMaintenanceSnapshot.Query()));
}
