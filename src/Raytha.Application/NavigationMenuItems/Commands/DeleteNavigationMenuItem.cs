using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.NavigationMenus.Commands;

namespace Raytha.Application.NavigationMenuItems.Commands;

public class DeleteNavigationMenuItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid NavigationMenuId { get; init; }

        public static Command Empty() => new() { NavigationMenuId = string.Empty };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.NavigationMenuId).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, _) =>
                    {
                        if (!db.NavigationMenus.Any(nm => nm.Id == request.NavigationMenuId.Guid))
                            throw new NotFoundException(
                                "Navigation Menu",
                                request.NavigationMenuId
                            );

                        if (!db.NavigationMenuItems.Any(nmi => nmi.Id == request.Id.Guid))
                            throw new NotFoundException("Navigation Menu Item", request.Id);
                    }
                );
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

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.NavigationMenuItems.FirstAsync(
                nmi => nmi.Id == request.Id.Guid,
                cancellationToken
            );

            await _mediator.Send(
                new CreateNavigationMenuRevision.Command
                {
                    NavigationMenuId = request.NavigationMenuId,
                },
                cancellationToken
            );

            _db.NavigationMenuItems.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
