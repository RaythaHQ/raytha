using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using CSharpVitamins;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplateByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<WidgetTemplateDto>>
    {
        /// <summary>
        /// The developer name of the widget type (e.g., "hero", "wysiwyg").
        /// </summary>
        public required string DeveloperName { get; init; }

        /// <summary>
        /// The theme ID to get the template for.
        /// </summary>
        public required ShortGuid ThemeId { get; init; }
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
            var entity = await _db
                .WidgetTemplates.Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .FirstOrDefaultAsync(
                    p =>
                        p.DeveloperName == request.DeveloperName
                        && p.ThemeId == request.ThemeId.Guid,
                    cancellationToken
                );

            if (entity == null)
                throw new NotFoundException(
                    "Widget Template",
                    $"{request.DeveloperName} for theme {request.ThemeId}"
                );

            return new QueryResponseDto<WidgetTemplateDto>(WidgetTemplateDto.GetProjection(entity)!);
        }
    }
}

