using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Webhooks.Commands;

/// <summary>
/// Fires a synthetic "webhook.test" event to a single webhook regardless of its subscriptions.
/// Returns the delivery id so the caller can poll its outcome.
/// </summary>
public class TestWebhook
{
    public const string EventName = "webhook.test";

    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IWebhookEventPublisher _publisher;
        private readonly ICurrentUser _currentUser;

        public Handler(
            IRaythaDbContext db,
            IWebhookEventPublisher publisher,
            ICurrentUser currentUser
        )
        {
            _db = db;
            _publisher = publisher;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var webhook = _db.Webhooks.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (webhook == null)
                throw new NotFoundException("Webhook", request.Id);

            var deliveryId = await _publisher.PublishToAsync(
                webhook.Id,
                EventName,
                new
                {
                    message = "Test event fired from the Raytha admin.",
                    webhookName = webhook.Name,
                    triggeredBy = _currentUser.EmailAddress,
                },
                cancellationToken
            );

            return new CommandResponseDto<ShortGuid>(deliveryId);
        }
    }
}
