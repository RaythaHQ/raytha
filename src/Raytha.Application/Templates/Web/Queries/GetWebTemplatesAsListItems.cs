using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Templates.Web.Queries;

public class GetWebTemplatesAsListItems
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<WebTemplateListItemDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.WebTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query
                    .Where(d =>
                        (d.Label.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.ToLower().Contains(searchQuery)));
            }

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(WebTemplateListItemDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<WebTemplateListItemDto>>(new ListResultDto<WebTemplateListItemDto>(items, total));
        }
    }
}
