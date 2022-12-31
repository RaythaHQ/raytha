using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Routes.Queries;

public class GetRouteByPath
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<RouteDto>>
    {
        public string Path { get; init; }
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<RouteDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<RouteDto> Handle(Query request)
        {
            var entity = _db.Routes
                .FirstOrDefault(p => p.Path.ToLower() == request.Path.ToLower());

            if (entity == null)
                throw new NotFoundException("Route", $"{request.Path} did not match any Route");

            return new QueryResponseDto<RouteDto>(RouteDto.GetProjection(entity));
        }
    }
}
