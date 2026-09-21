using Mediator;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Webhooks.Queries;

/// <summary>Lists every webhook event the running build can emit, grouped for the admin picker.</summary>
public class GetWebhookEvents
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyList<WebhookEventGroup>>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyList<WebhookEventGroup>>>
    {
        private readonly IWebhookEventCatalog _catalog;

        public Handler(IWebhookEventCatalog catalog)
        {
            _catalog = catalog;
        }

        public ValueTask<IQueryResponseDto<IReadOnlyList<WebhookEventGroup>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            return ValueTask.FromResult<IQueryResponseDto<IReadOnlyList<WebhookEventGroup>>>(
                new QueryResponseDto<IReadOnlyList<WebhookEventGroup>>(_catalog.Groups)
            );
        }
    }
}
