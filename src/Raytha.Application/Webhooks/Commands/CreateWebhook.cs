using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.Webhooks.Commands;

public class CreateWebhook
{
    public record Command : LoggableRequest<CommandResponseDto<CreatedWebhookDto>>
    {
        public string Name { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool IsActive { get; init; } = true;
        public string[] SubscribedEvents { get; init; } = Array.Empty<string>();
        public int MaxAttempts { get; init; } = 5;
        public int TimeoutSeconds { get; init; } = 30;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IWebhookEventCatalog catalog)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Url)
                .NotEmpty()
                .Must(BeAnAbsoluteHttpUrl)
                .WithMessage("Url must be an absolute http(s) URL.");
            RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 10);
            RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 120);
            RuleFor(x => x.SubscribedEvents)
                .NotEmpty()
                .WithMessage("Subscribe to at least one event (or \"*\").");
            RuleForEach(x => x.SubscribedEvents)
                .Must(catalog.IsKnownEvent)
                .WithMessage((_, e) => $"\"{e}\" is not a known webhook event.");
        }

        private static bool BeAnAbsoluteHttpUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<CreatedWebhookDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<CreatedWebhookDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var secret = WebhookSigner.GenerateSecret();

            var entity = new Webhook
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Url = request.Url,
                Description = request.Description,
                IsActive = request.IsActive,
                SubscribedEvents = request
                    .SubscribedEvents.Select(e => e.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToArray(),
                MaxAttempts = request.MaxAttempts,
                TimeoutSeconds = request.TimeoutSeconds,
                Secret = secret,
            };

            _db.Webhooks.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<CreatedWebhookDto>(
                new CreatedWebhookDto { Id = entity.Id, Secret = secret }
            );
        }
    }
}
