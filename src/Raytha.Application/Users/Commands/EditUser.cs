using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Users.Commands;

public class EditUser
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string EmailAddress { get; init; } = null!;
        public IEnumerable<ShortGuid> UserGroups { get; init; } = new List<ShortGuid>();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.Users.FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("User", request.Id);

                        if (request.EmailAddress.ToLower() != entity.EmailAddress.ToLower())
                        {
                            var emailAddressToCheck = request.EmailAddress.ToLower();
                            var doesAnotherEmailExist = db.Users.Any(p =>
                                p.EmailAddress.ToLower() == emailAddressToCheck
                            );
                            if (doesAnotherEmailExist)
                            {
                                context.AddFailure(
                                    "EmailAddress",
                                    "Another user with this email address already exists"
                                );
                                return;
                            }
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
            var entity = _db.Users.Include(p => p.UserGroups).First(p => p.Id == request.Id.Guid);

            //do not allow modifications to these elements of admins. (they should use admins screens)
            if (!entity.IsAdmin)
            {
                entity.FirstName = request.FirstName;
                entity.LastName = request.LastName;
                entity.EmailAddress = request.EmailAddress;
            }

            if (entity.UserGroups == null)
            {
                entity.UserGroups = new List<UserGroup>();
            }

            var newUserGroups =
                request.UserGroups != null ? request.UserGroups : new List<ShortGuid>();

            var currentUserGroupIds = entity.UserGroups.Select(p => (ShortGuid)p.Id);

            var userGroupsToAddIds = newUserGroups.Except(currentUserGroupIds);
            var userGroupsToDeleteIds = currentUserGroupIds.Except(newUserGroups);

            foreach (var userGroupToAddId in userGroupsToAddIds)
            {
                var userGroupToAdd = _db.UserGroups.First(p => p.Id == userGroupToAddId.Guid);
                entity.UserGroups.Add(userGroupToAdd);
            }

            foreach (var userGroupToDeleteId in userGroupsToDeleteIds)
            {
                entity.UserGroups.Remove(
                    entity.UserGroups.First(p => p.Id == userGroupToDeleteId.Guid)
                );
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
