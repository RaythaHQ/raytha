using System.Text.RegularExpressions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions;

/// <summary>
/// The optional public path an HTTP-trigger function answers at. Stored in Route.Path like the
/// paths of content items, views, and site pages: no leading or trailing slash.
/// </summary>
public static class RaythaFunctionRoutePath
{
    public const int MaxLength = 200;

    private static readonly Regex AllowedCharacters = new(@"^[A-Za-z0-9_./\-]+$", RegexOptions.Compiled);

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

    public static string Normalize(string? path) => (path ?? string.Empty).Trim().Trim('/');

    /// <summary>Why a normalized, non-empty path cannot be used, or null when it can.</summary>
    public static string? Problem(string path)
    {
        if (path.Length > MaxLength)
            return $"Public path must be {MaxLength} characters or fewer.";
        if (!AllowedCharacters.IsMatch(path))
            return "Public path may contain only letters, numbers, and - _ . /";
        if (path.Contains("//"))
            return "Public path cannot contain an empty segment (//).";
        if (path.Contains("..") || !path.IsValidRoutePath())
            return "Public path cannot contain '..' or a segment starting with '.', and only the last segment may contain a dot (for example llms.txt).";

        var root = path.Split('/')[0];
        if (ReservedRoots.Contains(root) || root.StartsWith("raytha_", StringComparison.OrdinalIgnoreCase))
            return $"Public path cannot start with \"{root}\"; Raytha reserves that path.";

        return null;
    }

    /// <summary>Why the path cannot be saved for this function, or null when it can.</summary>
    public static string? Problem(IRaythaDbContext db, string path, string triggerType, Guid? ownRouteId)
    {
        if (triggerType != RaythaFunctionTriggerType.HttpRequest.DeveloperName)
            return "Only HTTP request functions can have a public path.";

        var problem = Problem(path);
        if (problem != null)
            return problem;

        var lowered = path.ToLower();
        var taken = db.Routes.Any(r =>
            r.Path.ToLower() == lowered && (ownRouteId == null || r.Id != ownRouteId)
        );
        return taken ? $"The path \"{path}\" is already in use." : null;
    }

    /// <summary>Creates, moves, or deletes the function's route so it matches path; empty means none.</summary>
    public static void Apply(IRaythaDbContext db, RaythaFunction function, string path)
    {
        if (path.Length == 0)
        {
            if (function.Route != null)
            {
                db.Routes.Remove(function.Route);
                function.Route = null;
                function.RouteId = null;
            }
            return;
        }

        if (function.Route == null)
            function.Route = new Route { Path = path, RaythaFunctionId = function.Id };
        else
            function.Route.Path = path;
    }
}
