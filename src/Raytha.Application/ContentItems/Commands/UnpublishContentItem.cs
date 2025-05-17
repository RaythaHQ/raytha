using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentItems.Commands;

public class UnpublishContentItem
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;

        public Handler(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _db = db;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .ContentItems.Include(p => p.ContentType)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                entity.ContentType.DeveloperName
            );

            entity.IsPublished = false;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
