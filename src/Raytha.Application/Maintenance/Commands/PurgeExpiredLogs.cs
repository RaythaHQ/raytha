using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Maintenance.Commands;

/// <summary>
/// Applies each log's retention window. Not audit-logged: it runs unattended every day and
/// reports one summary line instead.
/// </summary>
public class PurgeExpiredLogs
{
    public record Command : IRequest<CommandResponseDto<IReadOnlyList<ClearedLogDto>>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<IReadOnlyList<ClearedLogDto>>>
    {
        private readonly IRaythaDbContext _db;
        private readonly ILogger<Handler> _logger;

        public Handler(IRaythaDbContext db, ILogger<Handler> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async ValueTask<CommandResponseDto<IReadOnlyList<ClearedLogDto>>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var purged = new List<ClearedLogDto>();
            var settings = await _db.OrganizationSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (settings is null)
            {
                return new CommandResponseDto<IReadOnlyList<ClearedLogDto>>(purged);
            }

            var now = DateTime.UtcNow;

            foreach (var log in RetainedLogs.All)
            {
                var cutoff = RetainedLogs.Cutoff(log.RetentionDays(settings), now);
                var deleted = cutoff is null ? 0 : await log.DeleteAsync(_db, cutoff, cancellationToken);
                purged.Add(new ClearedLogDto(log.Key, deleted));
            }

            _logger.LogInformation(
                "Log retention purge removed {Purged}",
                string.Join(", ", purged.Select(p => $"{p.Key}={p.Deleted}"))
            );

            return new CommandResponseDto<IReadOnlyList<ClearedLogDto>>(purged);
        }
    }
}
