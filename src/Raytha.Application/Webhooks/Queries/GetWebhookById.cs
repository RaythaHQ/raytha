using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Webhooks.Queries;

public class GetWebhookById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<WebhookDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebhookDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<WebhookDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.Webhooks.FirstOrDefaultAsync(
                p => p.Id == request.Id.Guid,
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("Webhook", request.Id);

            return new QueryResponseDto<WebhookDto>(WebhookDto.GetProjection(entity));
        }
    }
}
