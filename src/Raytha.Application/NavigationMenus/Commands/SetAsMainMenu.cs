using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.NavigationMenus.Commands;

public class SetAsMainMenu
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, _) =>
                    {
                        if (!db.NavigationMenus.Any(nm => nm.Id == request.Id.Guid))
                            throw new NotFoundException("Navigation Menu", request.Id);
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var previousMainNavigationMenu = await _db.NavigationMenus.FirstOrDefaultAsync(
                nm => nm.IsMainMenu,
                cancellationToken
            );

            if (previousMainNavigationMenu != null)
                previousMainNavigationMenu.IsMainMenu = false;

            var entity = await _db.NavigationMenus.FirstAsync(
                nm => nm.Id == request.Id.Guid,
                cancellationToken
            );

            entity.IsMainMenu = true;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
