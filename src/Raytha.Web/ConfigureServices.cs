using System.Text.Json;
using CSharpVitamins;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Domain.Entities;
using Raytha.Infrastructure.Persistence;
using Raytha.Web.Authentication;
using Raytha.Web.Filters;
using Raytha.Web.Middlewares;
using Raytha.Web.Services;
using RaythaZero.Web.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddWebUIServices(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.LoginPath = new PathString("/raytha/login-redirect");
                    options.Cookie.IsEssential = true;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.AccessDeniedPath = new PathString("/raytha/403");
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.EventsType = typeof(CustomCookieAuthenticationEvents);
                }
            );

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new IsAdminRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ManageUsersRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAdministratorsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                policy => policy.Requirements.Add(new ManageTemplatesRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
                policy => policy.Requirements.Add(new ManageContentTypesRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAuditLogsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageSystemSettingsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_MEDIA_ITEMS,
                policy => policy.Requirements.Add(new ManageMediaItemsRequirement())
            );
            options.AddPolicy(
                BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION,
                policy => policy.Requirements.Add(new ManageSitePagesRequirement())
            );
            options.AddPolicy(
                BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION
                        )
                    )
            );
            options.AddPolicy(
                BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION
                        )
                    )
            );
            options.AddPolicy(
                BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
                        )
                    )
            );

            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new ApiIsAdminRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageSystemSettingsRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageUsersRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageTemplatesRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_MEDIA_ITEMS,
                policy => policy.Requirements.Add(new ApiManageMediaItemsRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageContentTypesRequirement())
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ApiContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION
                        )
                    )
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ApiContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION
                        )
                    )
            );
            options.AddPolicy(
                RaythaApiAuthorizationHandler.POLICY_PREFIX
                    + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION,
                policy =>
                    policy.Requirements.Add(
                        new ApiContentTypePermissionRequirement(
                            BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION
                        )
                    )
            );
        });

        services.AddScoped<CustomCookieAuthenticationEvents>();
        services
            .AddControllersWithViews(options => { })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                o.JsonSerializerOptions.WriteIndented = true;
                o.JsonSerializerOptions.Converters.Add(new ShortGuidConverter());
                o.JsonSerializerOptions.Converters.Add(new AuditableUserDtoConverter());
            });
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentOrganization, CurrentOrganization>();
        services.AddScoped<IRelativeUrlBuilder, RelativeUrlBuilder>();
        services.AddScoped<IRenderEngine, RenderEngine>();
        services.AddScoped<IContentTypeInRoutePath, ContentTypeInRoutePath>();
        services.AddSingleton<IFileStorageProviderSettings, FileStorageProviderSettings>();
        services.AddSingleton<ICurrentVersion, CurrentVersion>();

        services.AddScoped<ForbidAccessIfRaythaFunctionsAreDisabledFilterAttribute>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminContentTypeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaApiAuthorizationHandler>();
        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            ApiKeyAuthorizationMiddleware
        >();

        services.AddScoped<ICsvService, CsvService>();

        services.AddRouting();
        services
            .AddDataProtection()
            .SetApplicationName("Raytha")
            .PersistKeysToDbContext<RaythaDbContext>();
        services.AddHttpContextAccessor();

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
        });
        services.AddRazorPages();
        return services;
    }
}
