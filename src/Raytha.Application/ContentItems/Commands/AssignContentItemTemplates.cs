using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Webhooks;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;

namespace Raytha.Application.ContentItems.Commands;

/// <summary>
/// Points content items at one template in the active theme. Omit <see cref="Command.ContentItemIds"/>
/// to move every item of the content type.
/// </summary>
[WebhookEvent(
    "content_item.templates_assigned",
    DisplayName = "Content item templates assigned",
    Group = "Content"
)]
public class AssignContentItemTemplates
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid ContentTypeId { get; init; }
        public ShortGuid TemplateId { get; init; }
        public IEnumerable<ShortGuid>? ContentItemIds { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.TemplateId == ShortGuid.Empty)
                        {
                            context.AddFailure("TemplateId", "Template is required.");
                            return;
                        }

                        var contentType = db.ContentTypes.FirstOrDefault(p =>
                            p.Id == request.ContentTypeId.Guid
                        );
                        if (contentType == null)
                            throw new NotFoundException("Content type", request.ContentTypeId);

                        var activeThemeId = db.OrganizationSettings.Select(os => os.ActiveThemeId).First();
                        var access = db
                            .WebTemplates.Include(wt => wt.TemplateAccessToModelDefinitions)
                            .Where(wt => wt.ThemeId == activeThemeId && wt.Id == request.TemplateId.Guid)
                            .Select(wt => wt.TemplateAccessToModelDefinitions)
                            .FirstOrDefault();
                        if (access == null)
                            throw new NotFoundException("Template", request.TemplateId);

                        if (!access.Any(p => p.ContentTypeId == contentType.Id))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "This template does not have access to this content type."
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
            var activeThemeId = await _db
                .OrganizationSettings.Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var items = _db.ContentItems.Where(p => p.ContentTypeId == request.ContentTypeId.Guid);
            var requestedIds = request.ContentItemIds?.Select(id => id.Guid).ToArray();
            if (requestedIds is { Length: > 0 })
            {
                items = items.Where(p => requestedIds.Contains(p.Id));
                var found = await items.CountAsync(cancellationToken);
                if (found != requestedIds.Length)
                    throw new NotFoundException("Content item", request.ContentTypeId);
            }

            var itemIds = await items.Select(p => p.Id).ToListAsync(cancellationToken);
            var relations = await _db
                .WebTemplateContentItemRelations.Where(wtr =>
                    itemIds.Contains(wtr.ContentItemId) && wtr.WebTemplate!.ThemeId == activeThemeId
                )
                .ToListAsync(cancellationToken);
            var relationByItem = relations
                .GroupBy(r => r.ContentItemId)
                .ToDictionary(g => g.Key, g => g.First());

            var tracked = await _db
                .ContentItems.Where(p => itemIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
            foreach (var item in tracked)
            {
                if (relationByItem.TryGetValue(item.Id, out var relation))
                {
                    relation.WebTemplateId = request.TemplateId.Guid;
                }
                else
                {
                    await _db.WebTemplateContentItemRelations.AddAsync(
                        new WebTemplateContentItemRelation
                        {
                            Id = Guid.NewGuid(),
                            ContentItemId = item.Id,
                            WebTemplateId = request.TemplateId.Guid,
                        },
                        cancellationToken
                    );
                }

                item.AddDomainEvent(new ContentItemUpdatedEvent(item));
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(request.ContentTypeId);
        }
    }
}
