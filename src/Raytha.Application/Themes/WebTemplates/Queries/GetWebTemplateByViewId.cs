using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateByViewId
{
    public record Query : IRequest<IQueryResponseDto<WebTemplateDto>>
    {
        public required ShortGuid ThemeId { get; init; }
        public required ShortGuid ViewId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<WebTemplateDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<WebTemplateDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var webTemplate = await _db.WebTemplateViewRelations
                .Where(wtr => wtr.ViewId == request.ViewId.Guid && wtr.WebTemplate!.ThemeId == request.ThemeId.Guid)
                .Include(wtr => wtr.WebTemplate)
                    .ThenInclude(wt => wt!.TemplateAccessToModelDefinitions)
                    .ThenInclude(md => md.ContentType)
                    .IncludeParentTemplates(wtr => wtr.WebTemplate!.ParentTemplate)
                .Select(wtr => wtr.WebTemplate)
                .FirstOrDefaultAsync(cancellationToken);

            if (webTemplate == null)
                throw new NotFoundException("WebTemplateViewRelation", request.ViewId);

            return new QueryResponseDto<WebTemplateDto>(WebTemplateDto.GetProjection(webTemplate)!);
        }
    }
}