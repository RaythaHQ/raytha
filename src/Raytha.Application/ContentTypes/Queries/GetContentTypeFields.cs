using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentTypes.Queries;

public class GetContentTypeFields
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<ContentTypeFieldDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.Ascending}";
        public ShortGuid ContentTypeId { get; init; } = ShortGuid.Empty;
        public string DeveloperName { get; init; } = null!;
        public bool ShowDeletedOnly { get; init; } = false;
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContentTypeFieldDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<ContentTypeFieldDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.ContentTypeFields.Include(p => p.ContentType).AsQueryable();

            if (request.ShowDeletedOnly)
            {
                query = query.IgnoreQueryFilters().Where(p => p.IsDeleted);
            }

            if (request.ContentTypeId != ShortGuid.Empty)
            {
                query = query.Where(p => p.ContentTypeId == request.ContentTypeId);
            }
            else if (!string.IsNullOrEmpty(request.DeveloperName.ToDeveloperName()))
            {
                query = query.Where(p =>
                    p.ContentType.DeveloperName == request.DeveloperName.ToDeveloperName()
                );
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(d =>
                    (
                        d.Label.ToLower().Contains(searchQuery)
                        || d.DeveloperName.Contains(searchQuery)
                    )
                );
            }

            var total = await query.CountAsync();
            var items = query
                .ApplyPaginationInput(request)
                .Select(ContentTypeFieldDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<ContentTypeFieldDto>>(
                new ListResultDto<ContentTypeFieldDto>(items, total)
            );
        }
    }
}
