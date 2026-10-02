using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Webhooks.Queries;

public class GetWebhooks
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebhookDto>>>
    {
        public bool? IsActive { get; init; }
        public override string OrderBy { get; init; } = $"Name {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebhookDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebhookDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Webhooks.AsQueryable();

            if (request.IsActive.HasValue)
                query = query.Where(p => p.IsActive == request.IsActive.Value);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) || p.Url.ToLower().Contains(search)
                );
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(WebhookDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WebhookDto>>(
                new ListResultDto<WebhookDto>(items, total)
            );
        }
    }
}
