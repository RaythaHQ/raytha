using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.Queries;

public class GetThemesAsListItems
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<ThemeListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"Title {SortOrder.ASCENDING}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ThemeListItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<ThemeListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var activeThemeId = await _db
                .OrganizationSettings.Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var query = _db.Themes.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(searchQuery)
                    || t.DeveloperName.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(t => ThemeListItemDto.GetProjection(t, activeThemeId))
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<ThemeListItemDto>>(
                new ListResultDto<ThemeListItemDto>(items, total)
            );
        }
    }
}

