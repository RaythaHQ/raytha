using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class RevertSitePage
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
            var revision = _db
                .SitePageRevisions.Include(p => p.SitePage)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (revision == null)
                throw new NotFoundException("Site Page Revision", request.Id);

            var sitePage = _db.SitePages.FirstOrDefault(p => p.Id == revision.SitePageId);

            if (sitePage == null)
                throw new BusinessException($"Site page is null {revision.SitePageId}");

            // If currently not in draft mode, save current published state as a revision
            if (!sitePage.IsDraft)
            {
                _db.SitePageRevisions.Add(
                    new SitePageRevision
                    {
                        SitePageId = sitePage.Id,
                        PublishedWidgets = sitePage.PublishedWidgets,
                    }
                );
                // Set published content to the revision's content
                sitePage.PublishedWidgets = revision.PublishedWidgets;
            }

            // Always update draft to match the revision
            sitePage.DraftWidgets = revision.PublishedWidgets;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(sitePage.Id);
        }
    }
}

