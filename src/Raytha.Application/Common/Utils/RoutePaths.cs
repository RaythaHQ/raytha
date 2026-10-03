using System.Text.RegularExpressions;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Utils;

public static class RoutePaths
{
    public const int MaxLength = 200;

    /// <summary>The one leading-dot segment the web relies on (RFC 8615), allowed only at the root.</summary>
    public const string WellKnownRoot = ".well-known";

    private static readonly Regex AllowedCharacters = new(@"^[A-Za-z0-9_./\-]+$", RegexOptions.Compiled);

    /// <summary>A typed path as stored: trimmed, with no outer slashes. Nothing else is rewritten.</summary>
    public static string Normalize(string? path) => (path ?? string.Empty).Trim().Trim('/');

    /// <summary>
    /// Why a normalized path cannot be a route, or null when it can. Shared by content items, views,
    /// site pages, and functions so every public path follows one rule.
    /// </summary>
    public static string? Problem(string path)
    {
        var problem = ShapeProblem(path);
        if (problem != null)
            return problem;

        var root = ReservedRoot(path);
        return root != null ? ReservedMessage(root) : null;
    }

    /// <summary>
    /// <see cref="Problem(string)"/> for an existing route: a route already on a reserved path may keep it.
    /// </summary>
    public static string? Problem(IRaythaDbContext db, string path, Guid currentRouteId)
    {
        var problem = ShapeProblem(path);
        if (problem != null)
            return problem;

        var root = ReservedRoot(db, path, currentRouteId);
        return root != null ? ReservedMessage(root) : null;
    }

    private static string? ShapeProblem(string path)
    {
        if (path.Length == 0)
            return "Route path is required.";
        if (path.Length > MaxLength)
            return $"Route path must be {MaxLength} characters or fewer.";
        if (!AllowedCharacters.IsMatch(path))
            return "Route path may contain only letters, numbers, and - _ . /";
        if (path.Contains("//"))
            return "Route path cannot contain an empty segment (//).";
        if (path.Contains("..") || !path.IsValidRoutePath())
            return $"Route path cannot contain '..' or a segment starting with '.', except a leading {WellKnownRoot}/ segment.";
        return null;
    }

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
