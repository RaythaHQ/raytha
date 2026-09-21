using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Commands;

public class ClearCompletedBackgroundTasks
{
    public record Command : LoggableRequest<CommandResponseDto<int>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<int>>
    {
        private readonly IRaythaRawDbCommands _dbCommands;

        public Handler(IRaythaRawDbCommands dbCommands)
        {
            _dbCommands = dbCommands;
        }

        public async ValueTask<CommandResponseDto<int>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var removed = await _dbCommands.ClearCompletedBackgroundTasksAsync(cancellationToken);
            return new CommandResponseDto<int>(removed);
        }
    }
}
