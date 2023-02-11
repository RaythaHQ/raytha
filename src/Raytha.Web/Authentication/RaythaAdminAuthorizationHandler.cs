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
using Raytha.Web.Utils;

namespace Raytha.Web.Authentication;

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

public class ManageMediaItemsRequirement : IAuthorizationRequirement
{
}

public class ContentTypePermissionRequirement : IAuthorizationRequirement
{
    public ContentTypePermissionRequirement(string permission) =>
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
            else if (requirement is ManageMediaItemsRequirement)
            {
                if (systemPermissionsClaims.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    if (contentTypePermissionsClaims.Any(p => p.EndsWith(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)))
                    {
                        context.Succeed(requirement);
                    }
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
                    string contentTypeDeveloperName = _httpContextAccessor.HttpContext.GetRouteValue(RouteConstants.CONTENT_TYPE_DEVELOPER_NAME) as string;

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