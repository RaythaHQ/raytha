using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Attributes;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.ContentItems.Queries;

public class GetDeletedContentItems
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<DeletedContentItemDto>>>
    {
        [ExcludePropertyFromOpenApiDocs]
        public string DeveloperName { get; init; }
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<DeletedContentItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<DeletedContentItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .DeletedContentItems.Include(p => p.ContentType)
                .Include(p => p.CreatorUser)
                .Where(p => p.ContentType.DeveloperName == request.DeveloperName.ToDeveloperName())
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(d =>
                    (
                        d.PrimaryField.ToLower().Contains(searchQuery)
                        || d.CreatorUser.FirstName.ToLower().Contains(searchQuery)
                        || d.CreatorUser.LastName.ToLower().Contains(searchQuery)
                    )
                );
            }

            var total = await query.CountAsync();
            var items = query
                .ApplyPaginationInput(request)
                .Select(DeletedContentItemDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<DeletedContentItemDto>>(
                new ListResultDto<DeletedContentItemDto>(items, total)
            );
        }
    }
}
