using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<WebTemplateDto>> { }

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
            var entity = await _db
                .WebTemplates.Include(p => p.TemplateAccessToModelDefinitions)
                .ThenInclude(p => p.ContentType)
                .IncludeParentTemplates(wt => wt.ParentTemplate)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Template", request.Id);

            var contentTypes = TemplateInsertVariables.ShowsContentVariables(
                entity.DeveloperName,
                entity.IsBuiltInTemplate
            )
                ? await _db
                    .ContentTypes.AsNoTracking()
                    .Include(p => p.ContentTypeFields)
                    .OrderBy(p => p.LabelSingular)
                    .ToListAsync(cancellationToken)
                : [];

            var dto = WebTemplateDto.GetProjection(entity)!;
            return new QueryResponseDto<WebTemplateDto>(
                dto with
                {
                    AvailableVariables = TemplateInsertVariables.ForWeb(
                        entity.DeveloperName,
                        entity.IsBuiltInTemplate,
                        contentTypes.Select(ContentTypeDto.GetProjection)
                    ),
                }
            );
        }
    }
}
