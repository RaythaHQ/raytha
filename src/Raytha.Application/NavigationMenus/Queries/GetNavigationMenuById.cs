using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.NavigationMenus.Queries;

public class GetNavigationMenuById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<NavigationMenuDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<NavigationMenuDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<NavigationMenuDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = await _db.NavigationMenus
                .FirstOrDefaultAsync(nm => nm.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Navigation Menu", request.Id);

            return new QueryResponseDto<NavigationMenuDto>(NavigationMenuDto.GetProjection(entity));
        }
    }
}