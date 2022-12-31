using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Admins.Commands;

public class UpdateRecentlyAccessedView
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid UserId { get; init; }
        public ShortGuid ContentTypeId { get; init; }
        public ShortGuid ViewId { get; init; }
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
            var entity = _db.Users
                    .FirstOrDefault(p => p.Id == request.UserId.Guid);

            if (entity == null)
                throw new NotFoundException("Admin", request.UserId.Guid);

            var updatedRecentlyAccessedViews = new List<RecentlyAccessedView>();
            
            if (!entity.RecentlyAccessedViews.Any(p => p.ContentTypeId == request.ContentTypeId.Guid))
            {
                updatedRecentlyAccessedViews.Add(new RecentlyAccessedView
                {
                    ContentTypeId = request.ContentTypeId,
                    ViewId = request.ViewId
                });
            }

            foreach (var item in entity.RecentlyAccessedViews)
            {
                if (item.ContentTypeId == request.ContentTypeId)
                {
                    updatedRecentlyAccessedViews.Add(new RecentlyAccessedView
                    {
                        ContentTypeId = item.ContentTypeId,
                        ViewId = request.ViewId
                    });
                }
                else
                {
                    updatedRecentlyAccessedViews.Add(item);
                }
            }

            entity.RecentlyAccessedViews = updatedRecentlyAccessedViews;
            _db.Users.Update(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
