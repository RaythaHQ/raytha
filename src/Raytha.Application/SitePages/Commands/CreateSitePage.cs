using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;

namespace Raytha.Application.SitePages.Commands;

public class CreateSitePage
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
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

            RuleFor(x => x.TemplateId)
                .NotEmpty()
                .WithMessage("Template is required.")
                .Must(templateId =>
                {
                    if (templateId == ShortGuid.Empty)
                        return false;

                    var template = db
                        .WebTemplates.Where(wt => wt.Id == templateId.Guid)
                        .Select(wt => new { wt.ThemeId })
                        .FirstOrDefault();

                    if (template == null)
                        return false;

                    var activeThemeId = db
                        .OrganizationSettings.Select(os => os.ActiveThemeId)
                        .First();

                    return template.ThemeId == activeThemeId;
                })
                .WithMessage("Template must exist and belong to the current active theme.");
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
            var newEntityId = Guid.NewGuid();
            var path = GetRoutePath(request.Title, newEntityId);

            var entity = new SitePage
            {
                Id = newEntityId,
                Title = request.Title,
                IsDraft = request.SaveAsDraft,
                IsPublished = !request.SaveAsDraft,
                WebTemplateId = request.TemplateId.Guid,
                Route = new Route { Path = path, SitePageId = newEntityId },
            };

            _db.SitePages.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);

            // Set RouteId after route is created
            entity.RouteId = entity.Route!.Id;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }

        private string GetRoutePath(string title, Guid entityId)
        {
            var path = title.ToUrlSlug().Truncate(200, string.Empty);

            if (string.IsNullOrEmpty(path))
            {
                path = ((ShortGuid)entityId).ToString();
            }

            // Case-insensitive check for existing routes
            if (_db.Routes.Any(p => p.Path.ToLower() == path.ToLower()))
            {
                path = $"{(ShortGuid)entityId}-{path}".Truncate(200, string.Empty);
            }

            return path;
        }
    }
}
