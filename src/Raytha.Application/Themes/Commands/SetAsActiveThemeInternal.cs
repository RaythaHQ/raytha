using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Commands;

public class SetAsActiveThemeInternal
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid ThemeId { get; init; }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var organizationSettings = await _db.OrganizationSettings
                .FirstAsync(cancellationToken);

            organizationSettings.ActiveThemeId = request.ThemeId.Guid;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.ThemeId.Guid);
        }
    }
}