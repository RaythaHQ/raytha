using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems.Commands;

public class SetAsHomePage
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
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
            var entity = _db.OrganizationSettings.First();

            var contentItemExists = _db.ContentItems.FirstOrDefault(p => p.Id == request.Id.Guid);
            if (contentItemExists == null)
                throw new NotFoundException("Content item not found", request.Id);

            entity.HomePageId = request.Id.Guid;
            entity.HomePageType = Route.CONTENT_ITEM_TYPE;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
