using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Views.Commands;

public class ToggleViewAsFavoriteForAdmin
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid UserId { get; init; }
        public ShortGuid ViewId { get; init; }
        public bool SetAsFavorite { get; init; }
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
            var entity = _db
                .Views.Include(p => p.UserFavorites)
                .FirstOrDefault(p => p.Id == request.ViewId.Guid);

            if (entity == null)
                throw new NotFoundException("View", request.ViewId);

            var user = _db.Users.FirstOrDefault(p => p.Id == request.UserId.Guid);
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            if (!request.SetAsFavorite)
            {
                if (
                    entity.UserFavorites != null
                    && entity.UserFavorites.Any(p => p.Id == request.UserId.Guid)
                )
                {
                    entity.UserFavorites.Remove(user);
                }
            }
            else
            {
                if (entity.UserFavorites == null)
                {
                    entity.UserFavorites = new List<User> { user };
                }
                else if (!entity.UserFavorites.Any(p => p.Id == request.UserId.Guid))
                {
                    entity.UserFavorites.Add(user);
                }
            }

            _db.Views.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
