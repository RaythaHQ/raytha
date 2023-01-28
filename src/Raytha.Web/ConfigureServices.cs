using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Security;
using Raytha.Domain.Entities;
using Raytha.Web.Filters;
using Raytha.Web.Helpers;
using Raytha.Web.Services;
using Raytha.Web.Utils;
using System;
using System.Text.Json;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Microsoft.AspNetCore.Mvc.Filters;

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


            options.AddPolicy(RaythaApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin,
                policy => policy.Requirements.Add(new ApiIsAdminRequirement()));
            options.AddPolicy(RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_USERS_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageUsersRequirement()));
            options.AddPolicy(RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION,
                policy => policy.Requirements.Add(new ApiManageContentTypesRequirement()));
            options.AddPolicy(RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION,
                policy => policy.Requirements.Add(new ApiContentTypePermissionRequirement(BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)));
            options.AddPolicy(RaythaApiAuthorizationHandler.POLICY_PREFIX + BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION,
                policy => policy.Requirements.Add(new ApiContentTypePermissionRequirement(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)));

        });

        services.AddScoped<SetFormValidationErrorsFilterAttribute>();
        services.AddScoped<CustomCookieAuthenticationEvents>();
        services.AddControllersWithViews(options =>
        {
            options.Filters.Add<SetFormValidationErrorsFilterAttribute>();
        }).AddJsonOptions(o => {
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
        services.AddSingleton<IFileStorageProviderSettings, FileStorageProviderSettings>();
        services.AddSingleton<ICurrentVersion, CurrentVersion>();
        services.AddScoped<IRenderEngine, RenderEngine>();
        services.AddScoped<GetOrSetRecentlyAccessedViewFilterAttribute>();
        services.AddScoped<SetPaginationInformationFilterAttribute>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiKeyAuthorizationMiddlewareResultHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaAdminContentTypeAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, RaythaApiAuthorizationHandler>();
        services.AddRouting();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => {
            c.DocumentFilter<LowercaseDocumentFilter>();
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Raytha API - V1",
                Version = "v1"
            });

            c.MapType<ShortGuid>(() => new OpenApiSchema { Type = "string" });

            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "X-API-KEY must appear in header",
                Type = SecuritySchemeType.ApiKey,
                Name = "X-API-KEY",
                In = ParameterLocation.Header,
                Scheme = "ApiKeyScheme"
            });
            var key = new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            };
            var requirement = new OpenApiSecurityRequirement
                    {
                             { key, new List<string>() }
                    };
            c.AddSecurityRequirement(requirement);
        });

        return services;
    }
}

public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = swaggerDoc.Paths.ToDictionary(entry => LowercaseEverythingButParameters(entry.Key),
            entry => entry.Value);
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var (key, value) in paths)
        {
            swaggerDoc.Paths.Add(key, value);
        }
    }

    private static string LowercaseEverythingButParameters(string key) => string.Join('/', key.Split('/').Select(x => x.Contains("{") ? x : x.ToLower()));
}



public class ShortGuidConverter : JsonConverter<ShortGuid>
{
    public override ShortGuid Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
            new ShortGuid(reader.GetString());

    public override void Write(
        Utf8JsonWriter writer,
        ShortGuid shortGuid,
        JsonSerializerOptions options)
    {
        if (shortGuid.Value == ShortGuid.Empty)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(shortGuid.Value);
        }
    }
}

public class AuditableUserDtoConverter : JsonConverter<AuditableUserDto>
{
    public override AuditableUserDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
            throw new NotImplementedException();

    public override void Write(
        Utf8JsonWriter writer,
        AuditableUserDto user,
        JsonSerializerOptions options)
    {
        if (user.Id.Value == ShortGuid.Empty)
        {
            writer.WriteNullValue();
        }
        else
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            jsonOptions.Converters.Add(new ShortGuidConverter());
            JsonSerializer.Serialize(writer, user, user.GetType(), jsonOptions);
        }
    }
}