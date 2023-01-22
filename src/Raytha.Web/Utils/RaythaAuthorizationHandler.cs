using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Raytha.Application.Common.Security;
using System;
using Raytha.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raytha.Application.Common.Utils;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using MediatR;
using Raytha.Application.Login.Commands;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Raytha.Web.Utils;

public class IsAdminRequirement : IAuthorizationRequirement
{
}

public class ManageUsersRequirement : IAuthorizationRequirement
{
}

public class ManageSystemSettingsRequirement : IAuthorizationRequirement
{
}

public class ManageContentTypesRequirement : IAuthorizationRequirement
{
}

public class ManageAdministratorsRequirement : IAuthorizationRequirement
{
}

public class ManageTemplatesRequirement : IAuthorizationRequirement
{
}

public class ManageAuditLogsRequirement : IAuthorizationRequirement
{
}

public class ContentTypePermissionRequirement : IAuthorizationRequirement
{
    public ContentTypePermissionRequirement(string permission) =>
        Permission = permission;

    public string Permission { get; }
}

public interface IHasApiKeyRequirement : IAuthorizationRequirement
{
}

public class ApiIsAdminRequirement : IHasApiKeyRequirement
{
}

public class ApiManageUsersRequirement : IHasApiKeyRequirement
{
}

public class ApiManageContentTypesRequirement : IHasApiKeyRequirement
{
}

public class ApiContentTypePermissionRequirement : IHasApiKeyRequirement
{
    public ApiContentTypePermissionRequirement(string permission) =>
        Permission = permission;

    public string Permission { get; }
}


public class RaythaAdminAuthorizationHandler : IAuthorizationHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = null;

    public RaythaAdminAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User == null || !context.User.Identity.IsAuthenticated || !IsAdmin(context.User))
        {
            return Task.CompletedTask;
        }
        
        var systemPermissionsClaims = context.User.Claims.Where(p => p.Type == RaythaClaimTypes.SystemPermissions).Select(p => p.Value).ToArray();
        var contentTypePermissionsClaims = context.User.Claims.Where(c => c.Type == RaythaClaimTypes.ContentTypePermissions).Select(p => p.Value).ToArray();
        
        var pendingRequirements = context.PendingRequirements.ToList();

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is IsAdminRequirement)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (requirement is ManageUsersRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_USERS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageSystemSettingsRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageContentTypesRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageTemplatesRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageAuditLogsRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_AUDIT_LOGS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ManageAdministratorsRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ContentTypePermissionRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    var permission = ((ContentTypePermissionRequirement)requirement).Permission;
                    string contentTypeDeveloperName = _httpContextAccessor.HttpContext.GetRouteValue("contentType") as string;
        
                    if (contentTypePermissionsClaims.Contains($"{contentTypeDeveloperName.ToDeveloperName()}_{permission}"))
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var isAdminClaim = user.Claims.FirstOrDefault(c => c.Type == RaythaClaimTypes.IsAdmin);
        if (isAdminClaim == null)
        {
            return false;
        }
        return Convert.ToBoolean(isAdminClaim.Value);
    }
}

public class RaythaAdminContentTypeAuthorizationHandler :
    AuthorizationHandler<OperationAuthorizationRequirement, string>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                   OperationAuthorizationRequirement requirement,
                                                   string resource)
    {
        if (context.User == null || !context.User.Identity.IsAuthenticated || !IsAdmin(context.User))
        {
            return Task.CompletedTask;
        }

        var contentTypePermissionsClaims = context.User.Claims.Where(c => c.Type == RaythaClaimTypes.ContentTypePermissions).Select(p => p.Value).ToArray();
        var systemPermissionsClaims = context.User.Claims.Where(p => p.Type == RaythaClaimTypes.SystemPermissions).Select(p => p.Value).ToArray();

        if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
        {
            context.Succeed(requirement);
        }
        else
        {
            if (contentTypePermissionsClaims.Contains($"{resource}_{requirement.Name}"))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        var isAdminClaim = user.Claims.FirstOrDefault(c => c.Type == RaythaClaimTypes.IsAdmin);
        if (isAdminClaim == null)
        {
            return false;
        }
        return Convert.ToBoolean(isAdminClaim.Value);
    }
}


public class RaythaApiAuthorizationHandler : IAuthorizationHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = null;
    private readonly IMediator _mediator;
    private const string X_API_KEY = "X-API-KEY";
    public const string POLICY_PREFIX = "API_";

    public RaythaApiAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IMediator mediator)
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.ToList();
        if (!pendingRequirements.Any(p => p is IHasApiKeyRequirement)) 
        {
            return;
        }
        byte[] errorBytes;
        if (!_httpContextAccessor.HttpContext.Request.Headers.Any(p => p.Key.ToUpper() == X_API_KEY))
        {
            errorBytes = Encoding.UTF8.GetBytes($"Missing Api Key");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }

        var apiKey = _httpContextAccessor.HttpContext.Request.Headers.FirstOrDefault(p => p.Key.ToUpper() == X_API_KEY).Value.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            errorBytes = Encoding.UTF8.GetBytes($"Missing Api Key");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }

        var user = await _mediator.Send(new LoginWithApiKey.Command { ApiKey = apiKey });

        if (!user.Success)
        {
            errorBytes = Encoding.UTF8.GetBytes($"Invalid Api Key: {user.Error}");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }


        var systemPermissions = new List<string>();
        var contentTypePermissions = new List<string>();

        foreach (var role in user.Result.Roles)
        {
            systemPermissions.AddRange(role.SystemPermissions);

            foreach (var contentTypePermission in role.ContentTypePermissionsFriendlyNames)
            {
                foreach (var granularPermission in contentTypePermission.Value)
                {
                    contentTypePermissions.Add($"{contentTypePermission.Key}_{granularPermission}");
                }
            }
        }

        foreach (var requirement in pendingRequirements)
        {
            if (requirement is ApiIsAdminRequirement)
            {
                context.Succeed(requirement);
            }

            if (requirement is ApiManageContentTypesRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiContentTypePermissionRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    var permission = ((ApiContentTypePermissionRequirement)requirement).Permission;
                    string contentTypeDeveloperName = _httpContextAccessor.HttpContext.GetRouteValue("contentType") as string;

                    if (contentTypePermissions.Contains($"{contentTypeDeveloperName.ToDeveloperName()}_{permission}"))
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }
    }
}

public class RaythaApiContentTypeAuthorizationHandler :
    AuthorizationHandler<OperationAuthorizationRequirement, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor = null;
    private readonly IMediator _mediator;
    private static string X_API_KEY = "X-API-KEY";

    public RaythaApiContentTypeAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IMediator mediator)
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                   OperationAuthorizationRequirement requirement,
                                                   string resource)
    {
        if (requirement is IHasApiKeyRequirement == false)
            return;

        byte[] errorBytes;
        if (!_httpContextAccessor.HttpContext.Request.Headers.Any(p => p.Key.ToUpper() == X_API_KEY))
        {
            errorBytes = Encoding.UTF8.GetBytes($"Missing Api Key");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }

        var apiKey = _httpContextAccessor.HttpContext.Request.Headers.FirstOrDefault(p => p.Key.ToUpper() == X_API_KEY).Value.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            errorBytes = Encoding.UTF8.GetBytes($"Missing Api Key");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }

        var user = await _mediator.Send(new LoginWithApiKey.Command { ApiKey = apiKey });

        if (!user.Success)
        {
            errorBytes = Encoding.UTF8.GetBytes($"Invalid Api Key: {user.Error}");
            _httpContextAccessor.HttpContext.Response.StatusCode = 401;
            _httpContextAccessor.HttpContext.Response.ContentType = "application/json";
            await _httpContextAccessor.HttpContext.Response.Body.WriteAsync(errorBytes, 0, errorBytes.Length);
            return;
        }

        var systemPermissions = new List<string>();
        var contentTypePermissions = new List<string>();

        foreach (var role in user.Result.Roles)
        {
            systemPermissions.AddRange(role.SystemPermissions);

            foreach (var contentTypePermission in role.ContentTypePermissionsFriendlyNames)
            {
                foreach (var granularPermission in contentTypePermission.Value)
                {
                    contentTypePermissions.Add($"{contentTypePermission.Key}_{granularPermission}");
                }
            }
        }

        if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
        {
            context.Succeed(requirement);
        }
        else
        {
            if (contentTypePermissions.Contains($"{resource}_{requirement.Name}"))
            {
                context.Succeed(requirement);
            }
        }
    }
}


public static class Operations
{
    public static OperationAuthorizationRequirement Read =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION };
    public static OperationAuthorizationRequirement Edit =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION };
    public static OperationAuthorizationRequirement Config =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION };
}

public class ApiKeyAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler
         DefaultHandler = new AuthorizationMiddlewareResultHandler();

    public async Task HandleAsync(
        RequestDelegate requestDelegate,
        HttpContext httpContext,
        AuthorizationPolicy authorizationPolicy,
        PolicyAuthorizationResult policyAuthorizationResult)
    {

        if (policyAuthorizationResult.Challenged && !policyAuthorizationResult.Succeeded && authorizationPolicy.Requirements.Any(requirement => requirement is IHasApiKeyRequirement))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        // Fallback to the default implementation.
        await DefaultHandler.HandleAsync(requestDelegate, httpContext, authorizationPolicy,
                               policyAuthorizationResult);
    }
}