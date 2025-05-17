using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.NavigationMenuItems.Queries;

public class GetNavigationMenuItemsByNavigationMenuId
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>>
    {
        public required ShortGuid NavigationMenuId { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var navigationMenuItems = await _db
                .NavigationMenuItems.Where(nmi =>
                    nmi.NavigationMenuId == request.NavigationMenuId.Guid
                )
                .OrderBy(nmi => nmi.CreationTime)
                .Select(NavigationMenuItemDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyCollection<NavigationMenuItemDto>>(
                navigationMenuItems
            );
        }
    }
}
