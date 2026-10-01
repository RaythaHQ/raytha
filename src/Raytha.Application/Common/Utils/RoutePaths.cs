using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.Common.Utils;

public static class RoutePaths
{
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
}
