using CSharpVitamins;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.UserGroups.Commands;

public class DeleteUserGroup
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.UserGroups.Include(p => p.Users).FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("UserGroup", request.Id);

                if (entity.Users.Any())
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Users are still assigned to this group. Unassign these users before deleting this group.");
                    return;
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.UserGroups.Include(p => p.Users).First(p => p.Id == request.Id.Guid);

            _db.UserGroups.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
