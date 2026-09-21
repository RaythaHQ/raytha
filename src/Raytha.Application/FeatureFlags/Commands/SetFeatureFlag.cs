using FluentValidation;
using Mediator;
using Raytha.Application.Common.Models;

namespace Raytha.Application.FeatureFlags.Commands;

public class SetFeatureFlag
{
    public record Command : LoggableRequest<CommandResponseDto<string>>
    {
        public string Key { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key)
                .NotEmpty()
                .Must(key => RaythaFeatureFlags.Find(key) is not null)
                .WithMessage(x => $"\"{x.Key}\" is not a known feature flag.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<string>>
    {
        private readonly IFeatureFlagService _featureFlags;

        public Handler(IFeatureFlagService featureFlags)
        {
            _featureFlags = featureFlags;
        }

        public async ValueTask<CommandResponseDto<string>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _featureFlags.SetAsync(request.Key, request.IsEnabled, cancellationToken);
            return new CommandResponseDto<string>(request.Key);
        }
    }
}
