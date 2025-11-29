using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class PublishSitePage
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>> { }

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
            var entity = await _db.SitePages.FirstOrDefaultAsync(
                p => p.Id == request.Id.Guid,
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("Site Page", request.Id);

            // Create a revision of the current published state before publishing new content
            if (!string.IsNullOrEmpty(entity._PublishedWidgetsJson))
            {
                _db.SitePageRevisions.Add(
                    new SitePageRevision
                    {
                        SitePageId = entity.Id,
                        _PublishedWidgetsJson = entity._PublishedWidgetsJson,
                    }
                );
            }

            // Copy draft widgets to published if there are draft changes
            if (entity.IsDraft && !string.IsNullOrEmpty(entity._DraftWidgetsJson))
            {
                entity._PublishedWidgetsJson = entity._DraftWidgetsJson;
                entity._DraftWidgetsJson = null;
            }

            entity.IsDraft = false;
            entity.IsPublished = true;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

