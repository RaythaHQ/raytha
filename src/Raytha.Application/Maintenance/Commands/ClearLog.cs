using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Commands;

/// <summary>Deletes every retained row of one log now, regardless of its retention window.</summary>
public class ClearLog
{
    public record Command : LoggableRequest<CommandResponseDto<ClearedLogDto>>
    {
        public string Key { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Key)
                .Must(key => RetainedLogs.Find(key) is not null)
                .WithMessage(p => $"'{p.Key}' is not a log that can be cleared.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ClearedLogDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ClearedLogDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var log = RetainedLogs.Find(request.Key)!;
            var deleted = await log.DeleteAsync(_db, olderThan: null, cancellationToken);
            return new CommandResponseDto<ClearedLogDto>(new ClearedLogDto(log.Key, deleted));
        }
    }
}

public record ClearedLogDto(string Key, int Deleted);
