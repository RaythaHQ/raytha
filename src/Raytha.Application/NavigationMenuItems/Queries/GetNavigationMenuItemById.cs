using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.NavigationMenuItems.Queries;

public class GetNavigationMenuItemById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<NavigationMenuItemDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<NavigationMenuItemDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<NavigationMenuItemDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = await _db.NavigationMenuItems
                .FirstOrDefaultAsync(nmi => nmi.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Navigation Menu Item", request.Id);

            return new QueryResponseDto<NavigationMenuItemDto>(NavigationMenuItemDto.GetProjection(entity));
        }
    }
}