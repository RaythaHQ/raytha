using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Queries;

/// <summary>
/// For every web template in a theme, what is bound to it: list views, content items, site pages
/// and child templates. Bindings are by id, so this is what a delete would break (or, for site
/// pages, remove). Content items are counted, with a sample of the first few by route path.
/// </summary>
public class GetThemeTemplateUsage
{
    public record Query : IRequest<IQueryResponseDto<ThemeTemplateUsageDto>>
    {
        public required ShortGuid ThemeId { get; init; }
    }

    public record ThemeTemplateUsageDto
    {
        public IReadOnlyList<TemplateUsage> Templates { get; init; } = [];
    }

    public record TemplateUsage
    {
        public required ShortGuid Id { get; init; }
        public required string Label { get; init; }
        public required string DeveloperName { get; init; }
        public required bool IsBaseLayout { get; init; }
        public required bool IsBuiltInTemplate { get; init; }

        /// <summary>True when anything below is bound to this template.</summary>
        public required bool InUse { get; init; }
        public IReadOnlyList<ViewUsage> Views { get; init; } = [];
        public required ContentItemUsage ContentItems { get; init; }
        public IReadOnlyList<SitePageUsage> SitePages { get; init; } = [];
        public IReadOnlyList<ChildTemplateUsage> ChildTemplates { get; init; } = [];
    }

    public record ViewUsage(
        ShortGuid Id,
        string Label,
        string DeveloperName,
        string ContentTypeDeveloperName
    );

    public record ContentItemUsage(int Count, IReadOnlyList<ContentItemReference> Sample);

    public record ContentItemReference(ShortGuid Id, string RoutePath, string ContentTypeDeveloperName);

    public record SitePageUsage(ShortGuid Id, string Title);

    public record ChildTemplateUsage(ShortGuid Id, string Label, string DeveloperName);

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ThemeTemplateUsageDto>>
    {
        private const int ContentItemSampleSize = 20;

        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ThemeTemplateUsageDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var themeId = request.ThemeId.Guid;
            if (!await _db.Themes.AnyAsync(t => t.Id == themeId, cancellationToken))
                throw new NotFoundException("Theme", request.ThemeId);

            var templates = await _db
                .WebTemplates.AsNoTracking()
                .Where(t => t.ThemeId == themeId)
                .OrderBy(t => t.Label)
                .Select(t => new
                {
                    t.Id,
                    t.Label,
                    t.DeveloperName,
                    t.IsBaseLayout,
                    t.IsBuiltInTemplate,
                    t.ParentTemplateId,
                })
                .ToListAsync(cancellationToken);
            var templateIds = templates.Select(t => t.Id).ToList();

            var views = (
                await _db
                    .WebTemplateViewRelations.AsNoTracking()
                    .Where(r => templateIds.Contains(r.WebTemplateId))
                    .Select(r => new
                    {
                        r.WebTemplateId,
                        r.ViewId,
                        r.View!.Label,
                        r.View.DeveloperName,
                        ContentTypeDeveloperName = r.View.ContentType!.DeveloperName,
                    })
                    .ToListAsync(cancellationToken)
            )
                .OrderBy(v => v.Label)
                .ToLookup(v => v.WebTemplateId);

            var itemCounts = await _db
                .WebTemplateContentItemRelations.AsNoTracking()
                .Where(r => templateIds.Contains(r.WebTemplateId))
                .GroupBy(r => r.WebTemplateId)
                .Select(g => new { WebTemplateId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.WebTemplateId, g => g.Count, cancellationToken);

            var samples = new Dictionary<Guid, List<ContentItemReference>>();
            foreach (var templateId in itemCounts.Keys)
            {
                samples[templateId] = (
                    await _db
                        .WebTemplateContentItemRelations.AsNoTracking()
                        .Where(r => r.WebTemplateId == templateId)
                        .OrderBy(r => r.ContentItem!.Route.Path)
                        .Take(ContentItemSampleSize)
                        .Select(r => new
                        {
                            r.ContentItemId,
                            RoutePath = r.ContentItem!.Route.Path,
                            ContentTypeDeveloperName = r.ContentItem.ContentType!.DeveloperName,
                        })
                        .ToListAsync(cancellationToken)
                )
                    .Select(i => new ContentItemReference(
                        i.ContentItemId,
                        i.RoutePath ?? "",
                        i.ContentTypeDeveloperName ?? ""
                    ))
                    .ToList();
            }

            var pages = (
                await _db
                    .SitePages.AsNoTracking()
                    .Where(p => templateIds.Contains(p.WebTemplateId))
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.WebTemplateId,
                    })
                    .ToListAsync(cancellationToken)
            )
                .OrderBy(p => p.Title)
                .ToLookup(p => p.WebTemplateId);

            var usage = templates
                .Select(t =>
                {
                    var templateViews = views[t.Id]
                        .Select(v => new ViewUsage(
                            v.ViewId,
                            v.Label ?? "",
                            v.DeveloperName ?? "",
                            v.ContentTypeDeveloperName ?? ""
                        ))
                        .ToList();
                    var itemCount = itemCounts.GetValueOrDefault(t.Id);
                    var templatePages = pages[t.Id].Select(p => new SitePageUsage(p.Id, p.Title)).ToList();
                    var children = templates
                        .Where(c => c.ParentTemplateId == t.Id)
                        .Select(c => new ChildTemplateUsage(c.Id, c.Label ?? "", c.DeveloperName ?? ""))
                        .ToList();

                    return new TemplateUsage
                    {
                        Id = t.Id,
                        Label = t.Label ?? "",
                        DeveloperName = t.DeveloperName ?? "",
                        IsBaseLayout = t.IsBaseLayout,
                        IsBuiltInTemplate = t.IsBuiltInTemplate,
                        InUse =
                            templateViews.Count > 0
                            || itemCount > 0
                            || templatePages.Count > 0
                            || children.Count > 0,
                        Views = templateViews,
                        ContentItems = new ContentItemUsage(
                            itemCount,
                            samples.GetValueOrDefault(t.Id) ?? []
                        ),
                        SitePages = templatePages,
                        ChildTemplates = children,
                    };
                })
                .ToList();

            return new QueryResponseDto<ThemeTemplateUsageDto>(
                new ThemeTemplateUsageDto { Templates = usage }
            );
        }
    }
}
