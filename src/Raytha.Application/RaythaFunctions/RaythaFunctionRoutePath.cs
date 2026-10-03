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
    public const int MaxLength = RoutePaths.MaxLength;

    public static string Normalize(string? path) => RoutePaths.Normalize(path);

    /// <summary>Why a normalized, non-empty path cannot be used, or null when it can.</summary>
    public static string? Problem(string path) =>
        RoutePaths.Problem(path)?.Replace("Route path", "Public path");

    /// <summary>Why the path cannot be saved for this function, or null when it can.</summary>
    public static string? Problem(IRaythaDbContext db, string path, string triggerType, Guid? ownRouteId)
    {
        if (triggerType != RaythaFunctionTriggerType.HttpRequest.DeveloperName)
            return "Only HTTP request functions can have a public path.";

        var problem = Problem(path);
        if (problem != null)
            return problem;

        return RoutePaths.IsTaken(db, path, ownRouteId)
            ? $"The path \"{path}\" is already in use."
            : null;
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
