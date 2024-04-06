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
        public override string OrderBy { get; init; } = $"LabelPlural {SortOrder.Ascending}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContentTypeDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<ContentTypeDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.ContentTypes
                .Include(p => p.ContentTypeFields)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query
                    .Where(d =>
                        (d.LabelPlural.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.Contains(searchQuery)));
            }

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(ContentTypeDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ContentTypeDto>>(new ListResultDto<ContentTypeDto>(items, total));
        }
    }
}
