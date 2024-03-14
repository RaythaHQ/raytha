using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.RaythaFunctions.Queries;

public class GetRaythaFunctions
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<RaythaFunctionDto>>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<RaythaFunctionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        protected override IQueryResponseDto<ListResultDto<RaythaFunctionDto>> Handle(Query request)
        {
            var query = _db.RaythaFunctions
                .Include(rf => rf.LastModifierUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(rf => 
                    rf.Name.ToLower().Contains(searchQuery) || 
                    rf.DeveloperName.ToLower().Contains(searchQuery));
            }

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(RaythaFunctionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<RaythaFunctionDto>>(new ListResultDto<RaythaFunctionDto>(items, total));
        }
    }
}
