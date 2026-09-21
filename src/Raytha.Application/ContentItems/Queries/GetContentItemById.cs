using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.ContentItems.Queries;

public class GetContentItemById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<ContentItemDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ContentItemDto>>
    {
        private readonly IRaythaDbJsonQueryEngine _db;
        private readonly IRaythaDbContext _context;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;

        public Handler(
            IRaythaDbJsonQueryEngine db,
            IRaythaDbContext context,
            IContentTypeInRoutePath contentTypeInRoutePath
        )
        {
            _db = db;
            _context = context;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }

        public async ValueTask<IQueryResponseDto<ContentItemDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db.FirstOrDefault(request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content item", request.Id);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                entity.ContentType.DeveloperName
            );

            var templateId = await _context
                .WebTemplateContentItemRelations.Where(relation => relation.ContentItemId == request.Id.Guid)
                .Select(relation => relation.WebTemplateId)
                .FirstOrDefaultAsync(cancellationToken);

            var dto = ContentItemDto.GetProjection(entity);
            if (templateId != Guid.Empty)
            {
                dto = dto with { WebTemplateId = templateId };
            }

            return new QueryResponseDto<ContentItemDto>(dto);
        }
    }
}
