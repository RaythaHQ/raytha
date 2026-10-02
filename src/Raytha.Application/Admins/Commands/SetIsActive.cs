using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;

namespace Raytha.Application.Admins.Commands;

public class SetIsActive
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool IsActive { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                        context.AddDenial(
                            db.AccountActionDenial(
                                currentUser,
                                request.Id.Guid,
                                request.IsActive
                                    ? AdminAccountAction.Restore
                                    : AdminAccountAction.Suspend
                            )
                        )
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
            var entity = _db.Users.FirstOrDefault(p => p.Id == request.Id.Guid && p.IsAdmin);
            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            entity.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
