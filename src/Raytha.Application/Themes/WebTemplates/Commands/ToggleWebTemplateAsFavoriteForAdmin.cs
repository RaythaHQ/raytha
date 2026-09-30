using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.WebTemplates.Commands;

public class ToggleWebTemplateAsFavoriteForAdmin
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public ShortGuid UserId { get; init; }
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
            var entity = await _db
                .WebTemplates.Include(p => p.UserFavorites)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Template", request.Id);

            var user = await _db.Users.FirstOrDefaultAsync(
                p => p.Id == request.UserId.Guid,
                cancellationToken
            );
            if (user == null)
                throw new NotFoundException("User", request.UserId);

            var isFavorite = entity.UserFavorites.Any(p => p.Id == user.Id);
            if (request.SetAsFavorite && !isFavorite)
            {
                entity.UserFavorites.Add(user);
            }
            else if (!request.SetAsFavorite && isFavorite)
            {
                entity.UserFavorites.Remove(entity.UserFavorites.First(p => p.Id == user.Id));
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
