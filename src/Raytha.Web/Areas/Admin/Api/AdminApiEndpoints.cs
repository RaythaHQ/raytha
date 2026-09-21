using Raytha.Application.Common.Security;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Wires <c>/raytha/api/auth</c> and <c>/raytha/api/admin</c>. Everything under <c>/admin</c>
/// requires an admin cookie session; each resource group narrows that further with the same
/// <c>BuiltInSystemPermission</c> / content-type policies the Razor pages use.
/// </summary>
public static class AdminApiEndpoints
{
    public const string AdminBasePath = "/raytha/api/admin";

    public static IEndpointRouteBuilder MapAdminApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAdminAuthApi();

        var admin = endpoints.MapGroup(AdminBasePath).RequireAuthorization(RaythaClaimTypes.IsAdmin);

        admin.MapDashboard();
        admin.MapUsers();
        admin.MapAdmins();
        admin.MapContentTypes();
        admin.MapContentItems();
        admin.MapMedia();
        admin.MapSitePages();
        admin.MapThemes();
        admin.MapEmailTemplates();
        admin.MapNavigationMenus();
        admin.MapRaythaFunctions();
        admin.MapSettings();

        return endpoints;
    }
}
