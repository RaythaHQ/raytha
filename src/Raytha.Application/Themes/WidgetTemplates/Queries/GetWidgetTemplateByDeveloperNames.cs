using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplateByDeveloperNames
{
    public record Query : IRequest<IQueryResponseDto<WidgetTemplateDto>>
    {
        public required string ThemeDeveloperName { get; init; }
        public required string TemplateDeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WidgetTemplateDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<WidgetTemplateDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var theme = await _db
                .Themes.Where(t => t.DeveloperName == request.ThemeDeveloperName.ToDeveloperName())
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (theme == null)
                throw new NotFoundException("Theme", request.ThemeDeveloperName);

            var widgetTemplate = await _db
                .WidgetTemplates.Include(wt => wt.LastModifierUser)
                .Include(wt => wt.CreatorUser)
                .FirstOrDefaultAsync(
                    wt =>
                        wt.DeveloperName == request.TemplateDeveloperName.ToDeveloperName()
                        && wt.ThemeId == theme.Id,
                    cancellationToken
                );

            if (widgetTemplate == null)
                throw new NotFoundException("Widget Template", request.TemplateDeveloperName);

            return new QueryResponseDto<WidgetTemplateDto>(
                WidgetTemplateDto.GetProjection(widgetTemplate)!
            );
        }
    }
}

