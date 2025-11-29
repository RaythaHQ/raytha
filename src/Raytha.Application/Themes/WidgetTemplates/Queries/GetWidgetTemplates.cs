using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplates
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WidgetTemplateDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
        public ShortGuid? ThemeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WidgetTemplateDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WidgetTemplateDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .WidgetTemplates.Include(wt => wt.LastModifierUser)
                .AsQueryable();

            if (request.ThemeId.HasValue)
            {
                query = query.Where(wt => wt.ThemeId == request.ThemeId.Value.Guid);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(wt =>
                    (wt.Label != null && wt.Label.ToLower().Contains(searchQuery))
                    || (wt.DeveloperName != null && wt.DeveloperName.ToLower().Contains(searchQuery))
                    || (
                        wt.LastModifierUser != null
                        && (
                            wt.LastModifierUser.FirstName.ToLower().Contains(searchQuery)
                            || wt.LastModifierUser.LastName.ToLower().Contains(searchQuery)
                        )
                    )
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(WidgetTemplateDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WidgetTemplateDto>>(
                new ListResultDto<WidgetTemplateDto>(items!, total)
            );
        }
    }
}

