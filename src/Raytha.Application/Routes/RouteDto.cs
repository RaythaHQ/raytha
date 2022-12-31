using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using System.Linq.Expressions;

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
                return "View";
            }
            else if (ContentItemId != ShortGuid.Empty)
            {
                return "ContentItem";
            }
            else
            {
                return "Unknown";
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
            Path = entity.Path
        };
    }
}
