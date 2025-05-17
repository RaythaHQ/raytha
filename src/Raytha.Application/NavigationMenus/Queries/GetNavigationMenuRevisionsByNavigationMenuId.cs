using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.NavigationMenus.Queries;

public class GetNavigationMenuRevisionsByNavigationMenuId
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<NavigationMenuRevisionDto>>>
    {
        public required ShortGuid NavigationMenuId { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<NavigationMenuRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<NavigationMenuRevisionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .NavigationMenuRevisions.Include(nmr => nmr.CreatorUser)
                .Where(nmr => nmr.NavigationMenuId == request.NavigationMenuId.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(NavigationMenuRevisionDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<NavigationMenuRevisionDto>>(
                new ListResultDto<NavigationMenuRevisionDto>(items, total)
            );
        }
    }
}
