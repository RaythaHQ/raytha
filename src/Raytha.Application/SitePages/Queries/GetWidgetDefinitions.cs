using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Queries;

/// <summary>
/// The widget templates of the active theme: built-ins in their standard order, then custom
/// templates by label.
/// </summary>
public class GetWidgetDefinitions
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var activeThemeId = await _db
                .OrganizationSettings.Select(o => o.ActiveThemeId)
                .FirstOrDefaultAsync(cancellationToken);

            var templates = await _db
                .WidgetTemplates.AsNoTracking()
                .Where(t => t.ThemeId == activeThemeId)
                .ToListAsync(cancellationToken);

            var builtInOrder = BuiltInWidgetType.WidgetTypes.Select(t => t.DeveloperName).ToList();
            var definitions = templates
                .OrderBy(t => builtInOrder.IndexOf(t.DeveloperName ?? string.Empty) is var i and >= 0 ? i : int.MaxValue)
                .ThenBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
                .Select(WidgetDefinitionDto.GetProjection)
                .ToList();

            return new QueryResponseDto<IReadOnlyList<WidgetDefinitionDto>>(definitions);
        }
    }
}
