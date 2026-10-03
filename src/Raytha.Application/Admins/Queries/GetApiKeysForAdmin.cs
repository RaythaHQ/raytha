using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Admins.Queries;

public class GetApiKeysForAdmin
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<ApiKeyDto>>>
    {
        public ShortGuid UserId { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ApiKeyDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<ApiKeyDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.ApiKeys.AsNoTracking().Where(p => p.UserId == request.UserId.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(ApiKeyDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<ApiKeyDto>>(
                new ListResultDto<ApiKeyDto>(items, total)
            );
        }
    }
}
