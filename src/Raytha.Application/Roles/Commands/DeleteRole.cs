using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;

namespace Raytha.Application.Roles.Commands;

[WebhookEvent("role.deleted", DisplayName = "Role deleted", Group = "Roles")]
public class DeleteRole
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
                        var role = db.FindRoleGrants([request.Id.Guid]).FirstOrDefault();
                        if (role == null)
                            throw new NotFoundException("Role", request.Id);

                        var denial = AdminAuthorityGuard.CheckRoleDeletion(
                            db.FindCaller(currentUser),
                            role
                        );
                        if (denial != null)
                        {
                            context.AddDenial(denial);
                            return;
                        }

                        if (db.Roles.Any(p => p.Id == role.Id && p.Users.Any()))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Users are still assigned to this role. Unassign these users before deleting this role."
                            );
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

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.Roles.Include(p => p.Users).First(p => p.Id == request.Id.Guid);

            _db.Roles.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
