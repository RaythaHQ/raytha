using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Commands;

public class SetAsActiveTheme
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
                if (!db.Themes.Any(rt => rt.Id == request.Id.Guid))
                    throw new NotFoundException("Theme", request.Id);
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IMediator _mediator;

        public Handler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            await _mediator.Send(new SetAsActiveThemeInternal.Command
            {
                ThemeId = request.Id.Guid,
            }, cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id.Guid);
        }
    }
}