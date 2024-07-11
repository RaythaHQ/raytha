using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateByDeveloperName
{
    public record Query : IRequest<IQueryResponseDto<WebTemplateDto>>
    {
        public required string DeveloperName { get; init; }
        public required ShortGuid ThemeId { get; init; }
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
            var webTemplate = await _db.WebTemplates
                .Include(wt => wt.TemplateAccessToModelDefinitions)
                    .ThenInclude(p => p.ContentType)
                .IncludeParentTemplates(wt => wt.ParentTemplate)
                .FirstOrDefaultAsync(wt => wt.DeveloperName == request.DeveloperName.ToDeveloperName() && wt.ThemeId == request.ThemeId.Guid, cancellationToken);

            if (webTemplate == null)
                throw new NotFoundException("Template", request.DeveloperName);

            return new QueryResponseDto<WebTemplateDto>(WebTemplateDto.GetProjection(webTemplate)!);
        }
    }
}
