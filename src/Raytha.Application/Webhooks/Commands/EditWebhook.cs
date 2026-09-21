using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Webhooks.Commands;

public class EditWebhook
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
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

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.Webhooks.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (entity == null)
                throw new NotFoundException("Webhook", request.Id);

            entity.Name = request.Name;
            entity.Url = request.Url;
            entity.Description = request.Description;
            entity.IsActive = request.IsActive;
            entity.SubscribedEvents = request
                .SubscribedEvents.Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToArray();
            entity.MaxAttempts = request.MaxAttempts;
            entity.TimeoutSeconds = request.TimeoutSeconds;

            _db.Webhooks.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
