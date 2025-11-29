using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.SitePages.Queries;

public class GetSitePageById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<SitePageDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<SitePageDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<SitePageDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .SitePages.Include(p => p.Route)
                .Include(p => p.WebTemplate)
                .Include(p => p.CreatorUser)
                .Include(p => p.LastModifierUser)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Site Page", request.Id);

            return new QueryResponseDto<SitePageDto>(SitePageDto.GetProjection(entity));
        }
    }
}

