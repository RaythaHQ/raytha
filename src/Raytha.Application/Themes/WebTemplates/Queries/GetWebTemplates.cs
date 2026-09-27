using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplates
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
        public bool BaseLayoutsOnly { get; init; } = false;
        public ShortGuid? ContentTypeId { get; init; }
        public ShortGuid? ThemeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebTemplateDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WebTemplates.AsQueryable();

            if (request.ThemeId.HasValue)
            {
                var themeId = request.ThemeId.Value.Guid;
                query = query.Where(wt => wt.ThemeId == themeId);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(wt =>
                    (wt.Label != null && wt.Label.ToLower().Contains(searchQuery))
                    || (wt.DeveloperName != null && wt.DeveloperName.ToLower().Contains(searchQuery))
                );
            }

            if (request.ContentTypeId is { } contentType && contentType.Guid != Guid.Empty)
            {
                var contentTypeId = contentType.Guid;
                query = query.Where(wt =>
                    wt.TemplateAccessToModelDefinitions.Any(c => c.ContentTypeId == contentTypeId)
                );
            }

            if (request.BaseLayoutsOnly)
            {
                query = query.Where(wt => wt.IsBaseLayout);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(WebTemplateDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WebTemplateDto>>(
                new ListResultDto<WebTemplateDto>(items!, total)
            );
        }
    }
}
