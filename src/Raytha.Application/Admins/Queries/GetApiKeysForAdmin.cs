using CSharpVitamins;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Admins.Queries;

public class GetApiKeysForAdmin
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ApiKeyDto>>>
    {
        public ShortGuid UserId { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.ASCENDING}";
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<ApiKeyDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ListResultDto<ApiKeyDto>> Handle(Query request)
        {
            var query = _db.ApiKeys.Where(p => p.UserId == request.UserId.Guid).AsQueryable();

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(ApiKeyDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<ApiKeyDto>>(new ListResultDto<ApiKeyDto>(items, total));
        }
    }
}
