using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateByDeveloperNames
{
    public record Query : IRequest<IQueryResponseDto<WebTemplateDto>>
    {
        public required string ThemeDeveloperName { get; init; }
        public required string TemplateDeveloperName { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebTemplateDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<WebTemplateDto>> Handle(
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

            var webTemplate = await _db
                .WebTemplates.Include(wt => wt.TemplateAccessToModelDefinitions)
                .ThenInclude(p => p.ContentType)
                .IncludeParentTemplates(wt => wt.ParentTemplate)
                .FirstOrDefaultAsync(
                    wt =>
                        wt.DeveloperName == request.TemplateDeveloperName.ToDeveloperName()
                        && wt.ThemeId == theme.Id,
                    cancellationToken
                );

            if (webTemplate == null)
                throw new NotFoundException("Template", request.TemplateDeveloperName);

            return new QueryResponseDto<WebTemplateDto>(WebTemplateDto.GetProjection(webTemplate)!);
        }
    }
}
