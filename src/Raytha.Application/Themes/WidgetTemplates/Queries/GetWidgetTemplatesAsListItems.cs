using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplatesAsListItems
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";

        /// <summary>
        /// Optional theme developer name. If not provided, falls back to the active theme.
        /// </summary>
        public string? ThemeDeveloperName { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            Guid themeId;

            if (!string.IsNullOrEmpty(request.ThemeDeveloperName))
            {
                var theme = await _db
                    .Themes.Where(t =>
                        t.DeveloperName == request.ThemeDeveloperName.ToDeveloperName()
                    )
                    .Select(t => new { t.Id })
                    .FirstOrDefaultAsync(cancellationToken);

                if (theme == null)
                    throw new NotFoundException("Theme", request.ThemeDeveloperName);

                themeId = theme.Id;
            }
            else
            {
                themeId = await _db
                    .OrganizationSettings.Select(os => os.ActiveThemeId)
                    .FirstAsync(cancellationToken);
            }

            var query = _db.WidgetTemplates.Where(wt => wt.ThemeId == themeId);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(wt =>
                    (
                        wt.Label!.ToLower().Contains(searchQuery)
                        || wt.DeveloperName!.ToLower().Contains(searchQuery)
                    )
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(WidgetTemplateListItemDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WidgetTemplateListItemDto>>(
                new ListResultDto<WidgetTemplateListItemDto>(items, total)
            );
        }
    }
}

