using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.AuditLogs.Commands;

public class ClearAllAuditLogs
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
            await _dbCommands.ClearAuditLogsAsync(cancellationToken);

            return new CommandResponseDto<string>("All audit logs have been cleared successfully.");
        }
    }
}

