using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class EditSitePage
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// The page title.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Whether to save as draft (true) or publish (false).
        /// </summary>
        public bool SaveAsDraft { get; init; }

        /// <summary>
        /// The web template ID to use for rendering.
        /// </summary>
        public ShortGuid TemplateId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");

            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var entity = db.SitePages.FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("Site Page", request.Id);

                        if (request.TemplateId == ShortGuid.Empty)
                        {
                            context.AddFailure("TemplateId", "Template is required.");
                            return;
                        }

                        var template = db
                            .WebTemplates.Where(wt => wt.Id == request.TemplateId.Guid)
                            .Select(wt => new { wt.ThemeId })
                            .FirstOrDefault();

                        if (template == null)
                        {
                            context.AddFailure("TemplateId", "Template not found.");
                            return;
                        }

                        var activeThemeId = db
                            .OrganizationSettings.Select(os => os.ActiveThemeId)
                            .First();

                        if (template.ThemeId != activeThemeId)
                        {
                            context.AddFailure(
                                "TemplateId",
                                "Template must belong to the current active theme."
                            );
                        }
                    }
                );
        }
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
                .SitePages.Include(p => p.Route)
                .First(p => p.Id == request.Id.Guid);

            entity.Title = request.Title;
            entity.WebTemplateId = request.TemplateId.Guid;

            if (request.SaveAsDraft)
            {
                // Save as draft - keep changes in draft state
                entity.IsDraft = true;
            }
            else
            {
                // Publish - create revision of current published state before publishing new content
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
                
                // Copy draft to published if there are draft changes
                if (entity.IsDraft && !string.IsNullOrEmpty(entity._DraftWidgetsJson))
                {
                    entity._PublishedWidgetsJson = entity._DraftWidgetsJson;
                    entity._DraftWidgetsJson = null;
                }
                entity.IsDraft = false;
                entity.IsPublished = true;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}

