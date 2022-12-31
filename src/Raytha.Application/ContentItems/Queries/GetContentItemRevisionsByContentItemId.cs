using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<ContentItemRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ListResultDto<ContentItemRevisionDto>> Handle(Query request)
        {
            var query = _db.ContentItemRevisions.AsQueryable()
                .Include(p => p.LastModifierUser)
                .Include(p => p.CreatorUser)
                .Where(p => p.ContentItemId == request.Id.Guid);

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(ContentItemRevisionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ContentItemRevisionDto>>(new ListResultDto<ContentItemRevisionDto>(items, total));
        }
    }
}
