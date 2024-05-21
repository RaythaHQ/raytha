using System.Text.Json;
using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenus.Commands;

public class RevertNavigationMenu
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, _) =>
            {
                if (!db.NavigationMenuRevisions.Any(p => p.Id == request.Id.Guid))
                    throw new NotFoundException("Navigation Menu Revision", request.Id);
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IMediator _mediator;

        public Handler(IRaythaDbContext db, IMediator mediator)
        {
            _db = db;
            _mediator = mediator;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var navigationMenuRevision = await _db.NavigationMenuRevisions
                .Include(nmr => nmr.NavigationMenu)
                .ThenInclude(nm => nm!.NavigationMenuItems)
                .FirstAsync(nmr => nmr.Id == request.Id.Guid, cancellationToken);

            var navigationMenu = navigationMenuRevision.NavigationMenu!;

            await _mediator.Send(new CreateNavigationMenuRevision.Command
            {
                NavigationMenuId = navigationMenu.Id,
            }, cancellationToken);

            _db.NavigationMenuItems.RemoveRange(navigationMenu.NavigationMenuItems);

            var navigationMenuItemsFromJson = JsonSerializer.Deserialize<ICollection<NavigationMenuItem>>(navigationMenuRevision.NavigationMenuItemsJson);
            await _db.NavigationMenuItems.AddRangeAsync(navigationMenuItemsFromJson!, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(navigationMenuRevision.NavigationMenuId);
        }
    }
}