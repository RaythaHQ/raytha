using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.RaythaFunctions.Queries;

public class GetRaythaFunctions
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<RaythaFunctionDto>>>
    { }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<RaythaFunctionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<RaythaFunctionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            IQueryable<RaythaFunction> query = _db
                .RaythaFunctions.AsNoTracking()
                .Include(rf => rf.LastModifierUser)
                .Include(rf => rf.Route);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(rf =>
                    rf.Name.ToLower().Contains(searchQuery)
                    || rf.DeveloperName.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(RaythaFunctionDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<RaythaFunctionDto>>(
                new ListResultDto<RaythaFunctionDto>(items, total)
            );
        }
    }
}
