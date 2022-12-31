using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentItems.Commands;

public class UnpublishContentItem
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
            var entity = _db.ContentItems
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            entity.IsPublished = false;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
