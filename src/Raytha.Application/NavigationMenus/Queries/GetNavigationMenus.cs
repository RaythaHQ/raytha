using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.NavigationMenus.Queries;

public class GetNavigationMenus
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<NavigationMenuDto>>> { }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<NavigationMenuDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<NavigationMenuDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.NavigationMenus.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(nm =>
                    nm.Label.ToLower().Contains(searchQuery)
                    || nm.DeveloperName.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(NavigationMenuDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<NavigationMenuDto>>(
                new ListResultDto<NavigationMenuDto>(items, total)
            );
        }
    }
}
