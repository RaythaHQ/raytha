using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.RaythaFunctions.Queries;

public class GetRaythaFunctionByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<RaythaFunctionDto>>
    {
        public required string DeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<RaythaFunctionDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<RaythaFunctionDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var developerName = request.DeveloperName.ToDeveloperName(allowDot: true);
            var entity = await _db
                .RaythaFunctions.AsNoTracking()
                .Include(rf => rf.Route)
                .FirstOrDefaultAsync(rf => rf.DeveloperName == developerName, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Raytha Function", request.DeveloperName);

            return new QueryResponseDto<RaythaFunctionDto>(RaythaFunctionDto.GetProjection(entity));
        }
    }
}

