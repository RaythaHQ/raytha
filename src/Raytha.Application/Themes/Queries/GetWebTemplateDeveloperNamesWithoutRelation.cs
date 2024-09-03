using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Queries;

public class GetWebTemplateDeveloperNamesWithoutRelation
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<string>>>
    {
        public required ShortGuid ThemeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<string>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<IReadOnlyCollection<string>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var activeThemeId = await _db.OrganizationSettings
                .Select(os => os.ActiveThemeId)
                .FirstAsync(cancellationToken);

            var activeThemeWebTemplateContentItemRelations = await _db.WebTemplateContentItemRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .Select(wtr => new { wtr.ContentItemId, WebTemplateDeveloperName = wtr.WebTemplate!.DeveloperName })
                .ToArrayAsync(cancellationToken);

            var activeThemeWebTemplateViewRelations = await _db.WebTemplateViewRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == activeThemeId)
                .Select(wtr => new { wtr.ViewId, WebTemplateDeveloperName = wtr.WebTemplate!.DeveloperName })
                .ToArrayAsync(cancellationToken);

            var newActiveThemeRelationsContentItemIds = await _db.WebTemplateContentItemRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == request.ThemeId.Guid)
                .Select(wtr => wtr.ContentItemId)
                .ToArrayAsync(cancellationToken);

            var newActiveThemeRelationViewIds = await _db.WebTemplateViewRelations
                .Where(wtr => wtr.WebTemplate!.ThemeId == request.ThemeId.Guid)
                .Select(wtr => wtr.ViewId)
                .ToArrayAsync(cancellationToken);

            var webTemplateDeveloperNamesFromContentItems = activeThemeWebTemplateContentItemRelations
                .Where(wtr => !newActiveThemeRelationsContentItemIds.Contains(wtr.ContentItemId))
                .Select(wtr => wtr.WebTemplateDeveloperName!)
                .ToArray();

            var webTemplateDeveloperNamesFromViews = activeThemeWebTemplateViewRelations
                .Where(wtr => !newActiveThemeRelationViewIds.Contains(wtr.ViewId))
                .Select(wtr => wtr.WebTemplateDeveloperName!)
                .ToArray();

            var webTemplateDeveloperNamesWithoutRelation = webTemplateDeveloperNamesFromContentItems
                .Union(webTemplateDeveloperNamesFromViews)
                .Distinct()
                .ToArray();

            return new QueryResponseDto<IReadOnlyCollection<string>>(webTemplateDeveloperNamesWithoutRelation);
        }
    }
}