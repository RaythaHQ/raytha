using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenuItems.Commands;

public class ReorderNavigationMenuItems
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public int Ordinal { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (!db.NavigationMenuItems.Any(nmi => nmi.Id == request.Id.Guid))
                            throw new NotFoundException("Navigation Menu Item", request.Id);

                        var parentNavigationMenuItemId = db
                            .NavigationMenuItems.Where(nmi => nmi.Id == request.Id.Guid)
                            .Select(nmi => nmi.ParentNavigationMenuItemId)
                            .First();

                        var itemsCount = db.NavigationMenuItems.Count(nmi =>
                            nmi.ParentNavigationMenuItemId == parentNavigationMenuItemId
                        );

                        if (request.Ordinal <= 0 || request.Ordinal > itemsCount)
                            context.AddFailure($"Invalid menu item order: {request.Ordinal}");
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

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .NavigationMenuItems.Include(nmi => nmi.NavigationMenu)
                .ThenInclude(nm => nm!.NavigationMenuItems)
                .FirstAsync(nmi => nmi.Id == request.Id.Guid, cancellationToken);

            var originalOrdinal = entity.Ordinal;
            if (originalOrdinal == request.Ordinal)
                return new CommandResponseDto<ShortGuid>(entity.Id);

            entity.Ordinal = request.Ordinal;
            var navigationMenuItemsToUpdate = new List<NavigationMenuItem> { entity };

            var navigationMenuItemsWithSameParent =
                entity.NavigationMenu!.NavigationMenuItems.Where(nmi =>
                    nmi.Id != entity.Id
                    && nmi.ParentNavigationMenuItemId == entity.ParentNavigationMenuItemId
                );

            if (request.Ordinal < originalOrdinal)
            {
                foreach (
                    var navigationMenuItem in navigationMenuItemsWithSameParent.Where(nmi =>
                        nmi.Ordinal >= request.Ordinal && nmi.Ordinal < originalOrdinal
                    )
                )
                {
                    navigationMenuItem.Ordinal += 1;
                    navigationMenuItemsToUpdate.Add(navigationMenuItem);
                }
            }
            else
            {
                foreach (
                    var navigationMenuItem in navigationMenuItemsWithSameParent.Where(nmi =>
                        nmi.Ordinal <= request.Ordinal && nmi.Ordinal > originalOrdinal
                    )
                )
                {
                    navigationMenuItem.Ordinal -= 1;
                    navigationMenuItemsToUpdate.Add(navigationMenuItem);
                }
            }

            _db.NavigationMenuItems.UpdateRange(navigationMenuItemsToUpdate);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
