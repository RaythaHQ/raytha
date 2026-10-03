using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.ContentItems.Queries;

public class GetContentItemRevisionsByContentItemId
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;

        public Handler(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _db = db;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .ContentItems.AsNoTracking()
                .Include(p => p.ContentType)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(
                entity.ContentType.DeveloperName
            );

            var query = _db
                .ContentItemRevisions.AsNoTracking()
                .Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .Where(p => p.ContentItemId == request.Id.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(ContentItemRevisionDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<ContentItemRevisionDto>>(
                new ListResultDto<ContentItemRevisionDto>(items, total)
            );
        }
    }
}
