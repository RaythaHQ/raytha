using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Commands;

public class ToggleThemeExportability
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required bool IsExportable { get; init; }

        public static Command Empty() => new()
        {
            IsExportable = false,
        };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, _) =>
            {
                if (!db.Themes.Any(rt => rt.Id == request.Id.Guid))
                    throw new NotFoundException("Theme", request.Id);
            });
        }
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
            var entity = await _db.Themes
                .FirstAsync(t => t.Id == request.Id.Guid, cancellationToken);

            entity.IsExportable = request.IsExportable;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}