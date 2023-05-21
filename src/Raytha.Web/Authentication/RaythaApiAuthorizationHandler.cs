using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Raytha.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Raytha.Application.Common.Utils;
using MediatR;
using Raytha.Application.Login.Commands;
using System.Collections.Generic;
using Raytha.Application.Common.Exceptions;
using Raytha.Web.Utils;

namespace Raytha.Web.Authentication;

public interface IHasApiKeyRequirement : IAuthorizationRequirement
{
}

public class ApiIsAdminRequirement : IHasApiKeyRequirement
{
}

public class ApiManageUsersRequirement : IHasApiKeyRequirement
{
}

public class ApiManageSystemSettingsRequirement : IHasApiKeyRequirement
{
}

public class ApiManageTemplatesRequirement : IHasApiKeyRequirement
{
}

public class ApiManageContentTypesRequirement : IHasApiKeyRequirement
{
}

public class ApiManageMediaItemsRequirement : IHasApiKeyRequirement
{
}

public class ApiContentTypePermissionRequirement : IHasApiKeyRequirement
{
    public ApiContentTypePermissionRequirement(string permission) =>
        Permission = permission;

    public string Permission { get; }
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
        if (!_httpContextAccessor.HttpContext.Request.Headers.Any(p => p.Key.ToUpper() == X_API_KEY))
        {
            throw new InvalidApiKeyException("Missing api key");
        }

        var apiKey = _httpContextAccessor.HttpContext.Request.Headers.FirstOrDefault(p => p.Key.ToUpper() == X_API_KEY).Value.FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidApiKeyException("Missing api key");
        }

        var user = await _mediator.Send(new LoginWithApiKey.Command { ApiKey = apiKey });

        if (!user.Success)
        {
            throw new InvalidApiKeyException(user.Error);
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
            else if (requirement is ApiManageSystemSettingsRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiManageUsersRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_USERS_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiManageTemplatesRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
            }
            else if (requirement is ApiManageMediaItemsRequirement)
            {
                if (systemPermissions.Contains(BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    if (contentTypePermissions.Any(p => p.EndsWith(BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)))
                    {
                        context.Succeed(requirement);
                    }
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
                    string contentTypeDeveloperName = _httpContextAccessor.HttpContext.GetRouteValue(RouteConstants.CONTENT_TYPE_DEVELOPER_NAME) as string;

                    if (contentTypePermissions.Contains($"{contentTypeDeveloperName.ToDeveloperName()}_{permission}"))
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }
    }
}