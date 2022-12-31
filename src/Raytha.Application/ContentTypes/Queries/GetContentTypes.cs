using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypes
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ContentTypeDto>>>
    {
        public string SearchQuery { get; init; } = string.Empty;
        public override string OrderBy { get; init; } = $"LabelPlural {SortOrder.Ascending}";
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<ContentTypeDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ListResultDto<ContentTypeDto>> Handle(Query request)
        {
            var query = _db.ContentTypes
                .Include(p => p.ContentTypeFields)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchQuery))
            {
                var searchQuery = request.SearchQuery.ToLower();
                query = query
                    .Where(d =>
                        (d.LabelPlural.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.Contains(searchQuery)));
            }

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(ContentTypeDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ContentTypeDto>>(new ListResultDto<ContentTypeDto>(items, total));
        }
    }
}
