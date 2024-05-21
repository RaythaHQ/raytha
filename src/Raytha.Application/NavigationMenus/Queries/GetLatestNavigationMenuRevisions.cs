using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Interfaces;

namespace Raytha.Application.NavigationMenus.Queries;

public class GetLatestNavigationMenuRevisions
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<NavigationMenuRevisionDto>>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<NavigationMenuRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<IReadOnlyCollection<NavigationMenuRevisionDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var latestNavigationMenuRevisions = await _db.NavigationMenuRevisions
                .GroupBy(nmr => nmr.NavigationMenuId)
                .Select(g => g.FirstOrDefault(r => r.CreationTime == g.Max(nmr => nmr.CreationTime)))
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyCollection<NavigationMenuRevisionDto>>(latestNavigationMenuRevisions
                .Select(NavigationMenuRevisionDto.GetProjection!)
                .ToArray());
        }
    }
}