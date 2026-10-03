using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Routes.Queries;

public class GetRouteByPath
{
    public record Query : IRequest<IQueryResponseDto<RouteDto>>
    {
        public string Path { get; init; } = string.Empty;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<RouteDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<RouteDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var path = string.IsNullOrEmpty(request.Path) ? string.Empty : request.Path.ToLower();
            var entity = await _db
                .Routes.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Path.ToLower() == path, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Route", $"{request.Path} did not match any Route");

            return new QueryResponseDto<RouteDto>(RouteDto.GetProjection(entity));
        }
    }
}
