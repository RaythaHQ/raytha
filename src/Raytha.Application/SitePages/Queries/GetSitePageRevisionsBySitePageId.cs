using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.SitePages.Queries;

public class GetSitePageRevisionsBySitePageId
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<SitePageRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<SitePageRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<SitePageRevisionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.SitePages.FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Site Page", request.Id);

            var query = _db
                .SitePageRevisions.AsQueryable()
                .Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .Where(p => p.SitePageId == request.Id.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(SitePageRevisionDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<SitePageRevisionDto>>(
                new ListResultDto<SitePageRevisionDto>(items, total)
            );
        }
    }
}

