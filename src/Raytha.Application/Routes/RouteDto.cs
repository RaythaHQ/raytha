using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Routes;

public record RouteDto : BaseEntityDto
{
    public string Path { get; init; } = string.Empty;
    public ShortGuid ViewId { get; init; }
    public ShortGuid ContentItemId { get; init; }
    public string PathType
    {
        get
        {
            if (ViewId != ShortGuid.Empty)
            {
                return Route.VIEW_TYPE;
            }
            else if (ContentItemId != ShortGuid.Empty)
            {
                return Route.CONTENT_ITEM_TYPE;
            }
            else
            {
                return Route.UNKNOWN_TYPE;
            }
        }
    }

    public static Expression<Func<Route, RouteDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static RouteDto GetProjection(Route entity)
    {
        if (entity == null)
            return null;

        return new RouteDto
        {
            Id = entity.Id,
            ViewId = entity.ViewId,
            ContentItemId = entity.ContentItemId,
            Path = entity.Path,
        };
    }
}
