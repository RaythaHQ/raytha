using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.NavigationMenus.Queries;

public class GetNavigationMenuByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<NavigationMenuDto>>
    {
        public required string DeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<NavigationMenuDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<NavigationMenuDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.NavigationMenus.FirstOrDefaultAsync(
                p => p.DeveloperName == request.DeveloperName.ToDeveloperName(),
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException(
                    "Navigation menu",
                    request.DeveloperName.ToDeveloperName()
                );

            return new QueryResponseDto<NavigationMenuDto>(NavigationMenuDto.GetProjection(entity));
        }
    }
}
