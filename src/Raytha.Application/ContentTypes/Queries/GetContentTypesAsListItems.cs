using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypesAsListItems
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ContentTypeListItemDto>>>
    {
        public override string OrderBy { get; init; } = $"LabelPlural {SortOrder.Ascending}";
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<ContentTypeListItemDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ListResultDto<ContentTypeListItemDto>> Handle(Query request)
        {
            var query = _db.ContentTypes
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query
                    .Where(d =>
                        (d.LabelPlural.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.Contains(searchQuery)));
            }

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(ContentTypeListItemDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ContentTypeListItemDto>>(new ListResultDto<ContentTypeListItemDto>(items, total));
        }
    }
}
