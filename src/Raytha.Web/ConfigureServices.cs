using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Domain.Entities;
using Raytha.Web.Filters;
using Raytha.Web.Helpers;
using Raytha.Web.Services;
using Raytha.Web.Utils;
using System;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddWebUIServices(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.LoginPath = new PathString("/raytha/login-redirect");
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
                options.AccessDeniedPath = new PathString("/raytha/forbidden");
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.EventsType = typeof(CustomCookieAuthenticationEvents);
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new IsAdminRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ManageUsersRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAdministratorsRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION,
                policy => policy.Requirements.Add(new ManageTemplatesRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
                policy => policy.Requirements.Add(new ManageContentTypesRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageAuditLogsRequirement()));
            options.AddPolicy(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION,
                policy => policy.Requirements.Add(new ManageSystemSettingsRequirement()));
            options.AddPolicy(BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION,
                policy => policy.Requirements.Add(new ContentTypePermissionRequirement(BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)));
            options.AddPolicy(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION,
                policy => policy.Requirements.Add(new ContentTypePermissionRequirement(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)));
            options.AddPolicy(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION,
                policy => policy.Requirements.Add(new ContentTypePermissionRequirement(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)));
        });

        services.AddScoped<SetFormValidationErrorsFilterAttribute>();
        services.AddScoped<CustomCookieAuthenticationEvents>();
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<SetFormValidationErrorsFilterAttribute>();
        });
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ICurrentOrganization, CurrentOrganization>();
        services.AddScoped<IRelativeUrlBuilder, RelativeUrlBuilder>();
        services.AddSingleton<IFileStorageProviderSettings, FileStorageProviderSettings>();
        services.AddScoped<IRenderEngine, RenderEngine>();
        services.AddScoped<GetOrSetRecentlyAccessedViewFilterAttribute>();
        services.AddScoped<SetPaginationInformationFilterAttribute>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminContentTypeAuthorizationHandler>();

        services.AddRouting();
        services.AddDataProtection();
        services.AddHttpContextAccessor();

        return services;
    }
}

