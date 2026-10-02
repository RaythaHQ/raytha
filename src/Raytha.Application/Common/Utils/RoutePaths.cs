using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Utils;

public static class RoutePaths
{
    /// <summary>First segments the host answers before the public catch-all, or serves from wwwroot.</summary>
    private static readonly HashSet<string> ReservedRoots = new(StringComparer.OrdinalIgnoreCase)
    {
        "raytha",
        "account",
        "api",
        "_static-files",
        "healthz",
        "favicon.ico",
    };

    public static bool IsTaken(IRaythaDbContext db, string path, Guid? exceptRouteId = null)
    {
        var loweredPath = path.ToLower();
        return db.Routes.Any(route =>
            route.Path.ToLower() == loweredPath
            && (exceptRouteId == null || route.Id != exceptRouteId.Value)
        );
    }

    public static bool IsTaken(IEnumerable<string> paths, string path) =>
        paths.Contains(path, StringComparer.OrdinalIgnoreCase);

    /// <summary>The first segment of <paramref name="path"/> when the host reserves it, otherwise null.</summary>
    public static string? ReservedRoot(string path)
    {
        var root = path.Trim().Trim('/').Split('/')[0];
        return ReservedRoots.Contains(root) || root.StartsWith("raytha_", StringComparison.OrdinalIgnoreCase)
            ? root
            : null;
    }

    /// <summary>
    /// The reserved root of <paramref name="path"/> when saving it would move the route onto it.
    /// A route already on that path keeps it, so a pre-2.0 path can still be saved unchanged.
    /// </summary>
    public static string? ReservedRoot(IRaythaDbContext db, string path, Guid currentRouteId)
    {
        var root = ReservedRoot(path);
        if (root == null)
            return null;

        var loweredPath = path.ToLower();
        var unchanged = db.Routes.Any(route =>
            route.Id == currentRouteId && route.Path.ToLower() == loweredPath
        );
        return unchanged ? null : root;
    }

    public static string ReservedMessage(string root) =>
        $"The route path cannot start with \"{root}\"; Raytha reserves that path.";

    /// <summary>Whether a generated path must fall back to its unique form.</summary>
    public static bool IsUnavailable(IRaythaDbContext db, string path) =>
        ReservedRoot(path) != null || IsTaken(db, path);
}
