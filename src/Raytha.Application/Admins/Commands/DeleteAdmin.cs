using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Admins.Commands;

public class DeleteAdmin
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser)
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                if (request.Id == currentUser.UserId)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "You cannot remove your own account.");
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
            var entity = _db.Users.FirstOrDefault(p => p.Id == request.Id.Guid && p.IsAdmin);
            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            var contentItems = _db.ContentItems.Where(p => p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid);

            if (contentItems.Any())
            {
                foreach (var contentItem in contentItems )
                {
                    contentItem.LastModifierUserId = null;
                    contentItem.CreatorUser = null;
                }
            }
            _db.ContentItems.UpdateRange(contentItems);

            var contentItemRevisions = _db.ContentItemRevisions.Where(p => p.CreatorUserId == request.Id.Guid || p.LastModifierUserId == request.Id.Guid);

            if (contentItems.Any())
            {
                foreach (var contentItemRevision in contentItemRevisions)
                {
                    contentItemRevision.LastModifierUserId = null;
                    contentItemRevision.CreatorUser = null;
                }
            }
            _db.ContentItemRevisions.UpdateRange(contentItemRevisions);

            _db.Users.Remove(entity);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
