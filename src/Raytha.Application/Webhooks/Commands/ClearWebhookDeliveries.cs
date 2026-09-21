using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Webhooks.Commands;

public class ClearWebhookDeliveries
{
    public record Command : LoggableRequest<CommandResponseDto<string>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<string>>
    {
        private readonly IRaythaRawDbCommands _dbCommands;

        public Handler(IRaythaRawDbCommands dbCommands)
        {
            _dbCommands = dbCommands;
        }

        public async ValueTask<CommandResponseDto<string>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _dbCommands.ClearWebhookDeliveriesAsync(cancellationToken);

            return new CommandResponseDto<string>(
                "All webhook deliveries have been cleared successfully."
            );
        }
    }
}
