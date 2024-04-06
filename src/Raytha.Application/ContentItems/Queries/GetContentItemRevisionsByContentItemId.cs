using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.ContentItems.Queries;

public class GetContentItemRevisionsByContentItemId
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;
        private readonly IContentTypeInRoutePath _contentTypeInRoutePath;
        public Handler(IRaythaDbContext db, IContentTypeInRoutePath contentTypeInRoutePath)
        {
            _db = db;
            _contentTypeInRoutePath = contentTypeInRoutePath;
        }

        public async Task<IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.ContentItems
                .Include(p => p.ContentType)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Content Item", request.Id);

            _contentTypeInRoutePath.ValidateContentTypeInRoutePathMatchesValue(entity.ContentType.DeveloperName);

            var query = _db.ContentItemRevisions.AsQueryable()
                .Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .Where(p => p.ContentItemId == request.Id.Guid);

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(ContentItemRevisionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ContentItemRevisionDto>>(new ListResultDto<ContentItemRevisionDto>(items, total));
        }
    }
}
