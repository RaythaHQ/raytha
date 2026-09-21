using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.EmailLogs.Commands;

public class ClearEmailLog
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
            await _dbCommands.ClearEmailLogsAsync(cancellationToken);

            return new CommandResponseDto<string>("The email log has been cleared successfully.");
        }
    }
}
