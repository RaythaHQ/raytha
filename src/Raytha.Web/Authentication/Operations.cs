using Microsoft.AspNetCore.Authorization.Infrastructure;
using Raytha.Domain.Entities;

namespace Raytha.Web.Authentication;

public static class Operations
{
    public static OperationAuthorizationRequirement Read =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION };
    public static OperationAuthorizationRequirement Edit =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION };
    public static OperationAuthorizationRequirement Config =
        new OperationAuthorizationRequirement { Name = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION };
}
