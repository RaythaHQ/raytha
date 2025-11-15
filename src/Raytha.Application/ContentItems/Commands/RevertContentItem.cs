using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.ContentItems.Commands;

public class RevertContentItem
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
                .ContentItemRevisions.Include(p => p.ContentItem)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item Revision", request.Id);

            var contentItem = _db
                .ContentItems.Include(p => p.ContentType)
                .FirstOrDefault(p => p.Id == entity.ContentItemId);

            if (contentItem == null)
                throw new BusinessException($"Content item is null {entity.ContentItemId}");

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                contentItem.ContentType.DeveloperName
            );

            if (!contentItem.IsDraft)
            {
                _db.ContentItemRevisions.Add(
                    new ContentItemRevision
                    {
                        ContentItemId = entity.ContentItemId,
                        PublishedContent = entity.PublishedContent,
                    }
                );
                contentItem.PublishedContent = entity.PublishedContent;
            }

            contentItem.DraftContent = entity.PublishedContent;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
