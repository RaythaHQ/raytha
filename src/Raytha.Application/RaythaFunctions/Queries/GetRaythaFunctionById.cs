using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.RaythaFunctions.Queries;

public class GetRaythaFunctionById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<RaythaFunctionDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<RaythaFunctionDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<RaythaFunctionDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.RaythaFunctions.FirstOrDefault(rf => rf.Id == request.Id.Guid);
            if (entity == null)
                throw new NotFoundException("Raytha Function", request.Id);

            return new QueryResponseDto<RaythaFunctionDto>(RaythaFunctionDto.GetProjection(entity));
        }
    }
}
