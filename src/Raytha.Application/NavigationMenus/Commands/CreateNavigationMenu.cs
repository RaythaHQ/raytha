using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.NavigationMenus.Commands;

public class CreateNavigationMenu
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required string Label { get; init; }
        public required string DeveloperName { get; init; }

        public static Command Empty() =>
            new() { Label = string.Empty, DeveloperName = string.Empty };
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (
                            db.NavigationMenus.Any(nm =>
                                nm.DeveloperName == request.DeveloperName.ToDeveloperName()
                            )
                        )
                            context.AddFailure(
                                "DeveloperName",
                                $"A menu with the developer name {request.DeveloperName.ToDeveloperName()} already exists."
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

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = new NavigationMenu
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToDeveloperName(),
            };

            await _db.NavigationMenus.AddAsync(entity, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
