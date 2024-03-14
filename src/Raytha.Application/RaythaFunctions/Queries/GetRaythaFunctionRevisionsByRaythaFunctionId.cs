using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions.Queries;

public class GetRaythaFunctionRevisionsByRaythaFunctionId
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        protected override IQueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>> Handle(Query request)
        {
            var query = _db.RaythaFunctionRevisions
                .Include(rfr => rfr.CreatorUser)
                .Where(rfr => rfr.RaythaFunctionId == request.Id.Guid);

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(RaythaFunctionRevisionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<RaythaFunctionRevisionDto>>(new ListResultDto<RaythaFunctionRevisionDto>(items, total));
        }
    }
}
