using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.Commands;

public class SetAsActiveThemeInternal
{
    public record Command : IRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid ThemeId { get; init; }
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
            var organizationSettings = await _db.OrganizationSettings.FirstAsync(cancellationToken);

            await BindMissingTemplateRelations(
                request.ThemeId.Guid,
                organizationSettings.ActiveThemeId,
                cancellationToken
            );

            organizationSettings.ActiveThemeId = request.ThemeId.Guid;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.ThemeId.Guid);
        }

        /// <summary>
        /// A theme created through the API has built-in templates and no rows tying existing
        /// views or content items to them. Rendering and public settings both assume those rows
        /// exist for the active theme, so activation creates the missing ones. It prefers the
        /// template developer name the item already used, then the built-in list or detail template.
        /// </summary>
        private async Task BindMissingTemplateRelations(
            Guid themeId,
            Guid previousThemeId,
            CancellationToken cancellationToken
        )
        {
            var templates = await _db
                .WebTemplates.Where(wt => wt.ThemeId == themeId)
                .Select(wt => new { wt.Id, wt.DeveloperName })
                .ToListAsync(cancellationToken);
            var templateIdByName = templates
                .Where(t => !string.IsNullOrEmpty(t.DeveloperName))
                .ToDictionary(t => t.DeveloperName!, t => t.Id);

            Guid Fallback(string developerName) =>
                templateIdByName.TryGetValue(developerName, out var id) ? id : Guid.Empty;

            var listTemplateId = Fallback(BuiltInWebTemplate.ContentItemListViewPage.DeveloperName);
            var detailTemplateId = Fallback(
                BuiltInWebTemplate.ContentItemDetailViewPage.DeveloperName
            );

            await BindViews(themeId, previousThemeId, templateIdByName, listTemplateId, cancellationToken);
            await BindContentItems(
                themeId,
                previousThemeId,
                templateIdByName,
                detailTemplateId,
                cancellationToken
            );
        }

        private async Task BindViews(
            Guid themeId,
            Guid previousThemeId,
            Dictionary<string, Guid> templateIdByName,
            Guid fallbackTemplateId,
            CancellationToken cancellationToken
        )
        {
            var bound = await _db
                .WebTemplateViewRelations.Where(r => r.WebTemplate!.ThemeId == themeId)
                .Select(r => r.ViewId)
                .ToListAsync(cancellationToken);
            var boundIds = bound.ToHashSet();
            var previous = await _db
                .WebTemplateViewRelations.Where(r => r.WebTemplate!.ThemeId == previousThemeId)
                .Select(r => new { r.ViewId, Name = r.WebTemplate!.DeveloperName })
                .ToListAsync(cancellationToken);
            var previousName = previous
                .GroupBy(r => r.ViewId)
                .ToDictionary(g => g.Key, g => g.First().Name);
            var viewIds = await _db.Views.Select(v => v.Id).ToListAsync(cancellationToken);

            var additions = new List<WebTemplateViewRelation>();
            foreach (var viewId in viewIds)
            {
                if (boundIds.Contains(viewId))
                    continue;
                var templateId = TemplateFor(previousName, templateIdByName, viewId, fallbackTemplateId);
                if (templateId == Guid.Empty)
                    continue;
                additions.Add(
                    new WebTemplateViewRelation
                    {
                        Id = Guid.NewGuid(),
                        ViewId = viewId,
                        WebTemplateId = templateId,
                    }
                );
            }

            if (additions.Count > 0)
                await _db.WebTemplateViewRelations.AddRangeAsync(additions, cancellationToken);
        }

        private async Task BindContentItems(
            Guid themeId,
            Guid previousThemeId,
            Dictionary<string, Guid> templateIdByName,
            Guid fallbackTemplateId,
            CancellationToken cancellationToken
        )
        {
            var bound = await _db
                .WebTemplateContentItemRelations.Where(r => r.WebTemplate!.ThemeId == themeId)
                .Select(r => r.ContentItemId)
                .ToListAsync(cancellationToken);
            var boundIds = bound.ToHashSet();
            var previous = await _db
                .WebTemplateContentItemRelations.Where(r => r.WebTemplate!.ThemeId == previousThemeId)
                .Select(r => new { r.ContentItemId, Name = r.WebTemplate!.DeveloperName })
                .ToListAsync(cancellationToken);
            var previousName = previous
                .GroupBy(r => r.ContentItemId)
                .ToDictionary(g => g.Key, g => g.First().Name);
            var itemIds = await _db.ContentItems.Select(p => p.Id).ToListAsync(cancellationToken);

            var additions = new List<WebTemplateContentItemRelation>();
            foreach (var itemId in itemIds)
            {
                if (boundIds.Contains(itemId))
                    continue;
                var templateId = TemplateFor(previousName, templateIdByName, itemId, fallbackTemplateId);
                if (templateId == Guid.Empty)
                    continue;
                additions.Add(
                    new WebTemplateContentItemRelation
                    {
                        Id = Guid.NewGuid(),
                        ContentItemId = itemId,
                        WebTemplateId = templateId,
                    }
                );
            }

            if (additions.Count > 0)
                await _db.WebTemplateContentItemRelations.AddRangeAsync(additions, cancellationToken);
        }

        private static Guid TemplateFor(
            Dictionary<Guid, string?> previousName,
            Dictionary<string, Guid> templateIdByName,
            Guid entityId,
            Guid fallbackTemplateId
        )
        {
            if (
                previousName.TryGetValue(entityId, out var name)
                && !string.IsNullOrEmpty(name)
                && templateIdByName.TryGetValue(name, out var matched)
            )
                return matched;
            return fallbackTemplateId;
        }
    }
}
