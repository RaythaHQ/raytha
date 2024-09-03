using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.Queries;

public class GetThemes
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ThemeDto>>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ThemeDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<ThemeDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.Themes
                .Include(t => t.CreatorUser)
                .Include(t => t.LastModifierUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(searchQuery));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query.ApplyPaginationInput(request).Select(ThemeDto.GetProjection()).ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<ThemeDto>>(new ListResultDto<ThemeDto>(items, total));
        }
    }
}