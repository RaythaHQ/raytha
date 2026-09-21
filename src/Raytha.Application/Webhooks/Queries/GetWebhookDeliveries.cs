using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Webhooks.Queries;

public class GetWebhookDeliveries
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebhookDeliveryDto>>>
    {
        public ShortGuid? WebhookId { get; init; }
        public string? EventName { get; init; }

        /// <summary>pending, succeeded, or failed.</summary>
        public string? Status { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebhookDeliveryDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebhookDeliveryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WebhookDeliveries.Include(p => p.Webhook).AsQueryable();

            if (request.WebhookId.HasValue && request.WebhookId != ShortGuid.Empty)
                query = query.Where(p => p.WebhookId == request.WebhookId.Value.Guid);

            if (!string.IsNullOrEmpty(request.EventName))
                query = query.Where(p => p.EventName == request.EventName);

            if (!string.IsNullOrEmpty(request.Status))
            {
                var status = WebhookDeliveryStatus.From(request.Status);
                query = query.Where(p => p.Status == status);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = (
                await query.ApplyPaginationInput(request).ToArrayAsync(cancellationToken)
            )
                .Select(WebhookDeliveryDto.GetProjection)
                .ToArray();

            return new QueryResponseDto<ListResultDto<WebhookDeliveryDto>>(
                new ListResultDto<WebhookDeliveryDto>(items, total)
            );
        }
    }
}
