using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.NavigationMenus.Commands;

public class DeleteNavigationMenu
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.NavigationMenus.FirstOrDefault(nm =>
                            nm.Id == request.Id.Guid
                        );

                        if (entity == null)
                            throw new NotFoundException("Navigation Menu", request.Id);

                        if (entity.IsMainMenu)
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete the main menu. Set another menu as the main menu before deleting this one."
                            );
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
            var entity = await _db.NavigationMenus.FirstAsync(
                nm => nm.Id == request.Id.Guid,
                cancellationToken
            );

            _db.NavigationMenus.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
