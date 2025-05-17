using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Users.Commands;

public class DeleteUser
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == currentUser.UserId)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete your own account."
                            );
                            return;
                        }

                        var entity = db.Users.FirstOrDefault(p => p.Id == request.Id.Guid);
                        if (entity == null)
                            throw new NotFoundException("User", request.Id);

                        if (entity.IsAdmin)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot delete another administrator account."
                            );
                            return;
                        }
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
            var entity = _db.Users.First(p => p.Id == request.Id.Guid);

            _db.Users.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
