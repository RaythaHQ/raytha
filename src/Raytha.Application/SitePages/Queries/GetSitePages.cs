using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Queries;

public class GetSitePages
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<SitePageDto>>>
    {
        public override string OrderBy { get; init; } = $"Title {SortOrder.Ascending}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<SitePageDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<SitePageDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .SitePages.Include(p => p.Route)
                .Include(p => p.WebTemplate)
                .Include(p => p.CreatorUser)
                .Include(p => p.LastModifierUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(d =>
                    d.Title.ToLower().Contains(searchQuery)
                    || (d.Route != null && d.Route.Path.ToLower().Contains(searchQuery))
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(sp => SitePageDto.GetProjection(sp))
                .ToArray();

            return new QueryResponseDto<ListResultDto<SitePageDto>>(
                new ListResultDto<SitePageDto>(items, total)
            );
        }
    }
}
