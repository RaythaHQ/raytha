using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks.Commands;

/// <summary>
/// Re-queues an existing delivery (same payload and signature) for another round of attempts.
/// </summary>
public class RedeliverWebhookDelivery
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IBackgroundTaskQueue _taskQueue;

        public Handler(IRaythaDbContext db, IBackgroundTaskQueue taskQueue)
        {
            _db = db;
            _taskQueue = taskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var delivery = _db.WebhookDeliveries.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (delivery == null)
                throw new NotFoundException("Webhook delivery", request.Id);

            if (!_db.Webhooks.Any(w => w.Id == delivery.WebhookId))
            {
                return new CommandResponseDto<ShortGuid>(
                    "Id",
                    "The webhook for this delivery no longer exists."
                );
            }

            delivery.Status = WebhookDeliveryStatus.Pending;
            delivery.AttemptCount = 0;
            delivery.NextRetryAt = null;
            delivery.CompletionTime = null;
            delivery.ErrorMessage = null;
            delivery.ResponseCode = null;
            delivery.ResponseBody = null;
            _db.WebhookDeliveries.Update(delivery);
            await _db.SaveChangesAsync(cancellationToken);

            await _taskQueue.EnqueueAsync<DeliverWebhookTask>(
                new DeliverWebhookTask.Args { DeliveryId = delivery.Id },
                cancellationToken
            );

            return new CommandResponseDto<ShortGuid>(delivery.Id);
        }
    }
}
