using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.ContentTypes.Queries;

namespace Raytha.Web.AdminSpa;

/// <summary>
/// Sends bookmarks and links from the 1.5 Razor admin to the SPA page that replaced them, e.g.
/// <c>/raytha/posts/edit/{id}</c> to <c>/raytha/content/posts/items/{id}</c>. Only GET pages are
/// mapped; 1.5 action URLs (delete, suspend, ...) have no SPA page and keep answering 404.
/// </summary>
public static class LegacyAdminRedirects
{
    private const string ContentTypeParameter = "{contentType}";

    /// <summary>
    /// 1.5 routes relative to <c>/raytha</c>, first match wins. <c>{contentType}</c> in the first
    /// segment matches only an existing content type whose name is not in <see cref="ReservedSegments"/>.
    /// </summary>
    public static readonly (string From, string To)[] Routes =
    [
        ("audit-logs", "audit-log"),
        ("content-types/create", "content-types/new"),
        ("email-templates/edit/{id}", "email-templates/{id}"),
        ("email-templates/revisions/{id}", "email-templates/{id}"),
        ("functions/create", "functions/new"),
        ("functions/edit/{id}", "functions/{id}"),
        ("functions/edit/{id}/revisions", "functions/{id}"),
        ("menus/create", "menus/new"),
        ("menus/edit/{id}", "menus/{id}"),
        ("menus/edit/{menuId}/menu-items/edit/{id}", "menus/{menuId}/items/{id}"),
        ("menus/edit/{menuId}/menu-items/reorder", "menus/{menuId}"),
        ("menus/edit/{menuId}/menu-items/reorder/{parentId}", "menus/{menuId}"),
        ("menus/{menuId}/menu-items", "menus/{menuId}"),
        ("menus/{menuId}/menu-items/create", "menus/{menuId}/items/new"),
        ("profile/change-password", "profile"),
        ("settings/admins/create", "settings/admins/new"),
        ("settings/admins/edit/{id}", "settings/admins/{id}"),
        ("settings/admins/apikeys/{id}", "settings/admins/{id}"),
        ("settings/authentication-schemes", "settings/authentication"),
        ("settings/authentication-schemes/create", "settings/authentication"),
        ("settings/authentication-schemes/edit/{id}", "settings/authentication/{id}"),
        ("settings/roles/create", "settings/roles/new"),
        ("settings/roles/edit/{id}", "settings/roles/{id}"),
        ("site-pages/create", "site-pages/new"),
        ("site-pages/edit/{id}", "site-pages/{id}"),
        ("site-pages/edit/{id}/revisions", "site-pages/{id}"),
        ("site-pages/layout/{id}", "site-pages/{id}/layout"),
        ("site-pages/{id}/widget/edit", "site-pages/{id}/layout"),
        ("themes/create", "themes/new"),
        ("themes/duplicate", "themes"),
        ("themes/import", "themes"),
        ("themes/edit/{id}", "themes/{id}"),
        ("themes/edit/{id}/media-items", "themes/{id}/assets"),
        ("themes/edit/{id}/web-templates", "themes/{id}/web-templates"),
        ("themes/edit/{id}/web-templates/create", "themes/{id}/web-templates/new"),
        ("themes/edit/{themeId}/web-templates/edit/{id}", "themes/{themeId}/web-templates/{id}"),
        ("themes/edit/{themeId}/web-templates/edit/{id}/revisions", "themes/{themeId}/web-templates/{id}"),
        ("themes/edit/{id}/widget-templates", "themes/{id}/widget-templates"),
        ("themes/edit/{themeId}/widget-templates/edit/{id}", "themes/{themeId}/widget-templates/{id}"),
        ("themes/edit/{themeId}/widget-templates/edit/{id}/revisions", "themes/{themeId}/widget-templates/{id}"),
        ("users/create", "users/new"),
        ("users/edit/{id}", "users/{id}"),
        ("users/groups/create", "users/groups/new"),
        ("users/groups/edit/{id}", "users/groups/{id}"),
        ("{contentType}", "content/{contentType}"),
        ("{contentType}/create", "content/{contentType}/new"),
        ("{contentType}/edit/{id}", "content/{contentType}/items/{id}"),
        ("{contentType}/edit/{id}/revisions", "content/{contentType}/items/{id}"),
        ("{contentType}/edit/{id}/settings", "content/{contentType}/items/{id}"),
        ("{contentType}/begin-import-from-csv", "content/{contentType}/import"),
        ("{contentType}/configuration", "content-types/{contentType}/configuration"),
        ("{contentType}/fields", "content-types/{contentType}/fields"),
        ("{contentType}/fields/create", "content-types/{contentType}/fields/new"),
        ("{contentType}/fields/reorder", "content-types/{contentType}/fields"),
        ("{contentType}/fields/edit/{id}", "content-types/{contentType}/fields/{id}"),
        ("{contentType}/trash", "content-types/{contentType}/trash"),
        ("{contentType}/views", "content/{contentType}/views"),
        ("{contentType}/views/create", "content/{contentType}/views/new"),
        ("{contentType}/views/edit/{id}", "content/{contentType}/views/{id}"),
        ("{contentType}/views/columns/{viewId}", "content/{contentType}/views/{viewId}#columns"),
        ("{contentType}/views/filter/{viewId}", "content/{contentType}/views/{viewId}#filter"),
        ("{contentType}/views/sort/{viewId}", "content/{contentType}/views/{viewId}#sort"),
        ("{contentType}/views/public-settings/{viewId}", "content/{contentType}/views/{viewId}#public"),
        ("{contentType}/{viewId}", "content/{contentType}/{viewId}"),
    ];

    /// <summary>
    /// First segments the SPA, the server, or a 1.5 literal route owns. A content type with one of
    /// these names was already unreachable at <c>/raytha/{name}</c> in 1.5.
    /// </summary>
    public static readonly HashSet<string> ReservedSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "api",
        "assets",
        "audit-log",
        "audit-logs",
        "background-tasks",
        "content",
        "content-types",
        "edit",
        "email-log",
        "email-templates",
        "error",
        "functions",
        "login",
        "login-redirect",
        "logout",
        "maintenance",
        "media",
        "media-items",
        "menus",
        "profile",
        "relationship",
        "settings",
        "setup",
        "site-pages",
        "themes",
        "users",
        "webhooks",
    };

    public readonly record struct Target(string Path, string? ContentType);

    /// <summary>
    /// The SPA path (relative to <c>/raytha</c>) for a 1.5 admin path. When <see cref="Target.ContentType"/>
    /// is set the redirect applies only if that content type exists.
    /// </summary>
    public static Target? Resolve(PathString path)
    {
        if (!path.StartsWithSegments(AdminSpaExtensions.BasePath, out var rest) || !rest.HasValue)
        {
            return null;
        }

        var segments = rest.Value!.Trim('/').Split('/');
        if (segments.Any(string.IsNullOrEmpty))
        {
            return null;
        }

        foreach (var (from, to) in Routes)
        {
            var pattern = from.Split('/');
            if (pattern.Length != segments.Length)
            {
                continue;
            }

            var values = new Dictionary<string, string>();
            var matched = true;
            for (var i = 0; i < pattern.Length && matched; i++)
            {
                if (pattern[i].StartsWith('{'))
                {
                    values[pattern[i]] = segments[i];
                }
                else
                {
                    matched = string.Equals(pattern[i], segments[i], StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!matched)
            {
                continue;
            }

            string? contentType = null;
            if (pattern[0] == ContentTypeParameter)
            {
                contentType = segments[0];
                if (ReservedSegments.Contains(contentType) || !IsDeveloperName(contentType))
                {
                    continue;
                }
            }

            var hashIndex = to.IndexOf('#');
            var toPath = hashIndex < 0 ? to : to[..hashIndex];
            var target = string.Join(
                '/',
                toPath.Split('/').Select(part => values.TryGetValue(part, out var value) ? Uri.EscapeDataString(value) : part)
            );
            return new Target(hashIndex < 0 ? target : target + to[hashIndex..], contentType);
        }

        return null;
    }

    public static IApplicationBuilder UseLegacyAdminRedirects(this IApplicationBuilder app) =>
        app.Use(
            async (context, next) =>
            {
                var request = context.Request;
                if (
                    (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
                    && Resolve(request.Path) is { } target
                    && (target.ContentType is null || await ContentTypeExists(context, target.ContentType))
                )
                {
                    context.Response.Redirect($"{request.PathBase}{AdminSpaExtensions.BasePath}/{target.Path}");
                    return;
                }

                await next();
            }
        );

    private static bool IsDeveloperName(string segment) =>
        segment.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_');

    private static async Task<bool> ContentTypeExists(HttpContext context, string developerName)
    {
        try
        {
            await context
                .RequestServices.GetRequiredService<ISender>()
                .Send(new GetContentTypeByDeveloperName.Query { DeveloperName = developerName }, context.RequestAborted);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
