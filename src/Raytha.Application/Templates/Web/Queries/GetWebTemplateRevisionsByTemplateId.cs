using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Application.Templates.Email;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Templates.Web.Queries;

public class GetWebTemplateRevisionsByTemplateId
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>> Handle(Query request)
        {
            var query = _db.WebTemplateRevisions.AsQueryable()
                .Include(p => p.WebTemplate)
                .Include(p => p.CreatorUser)
                .Where(p => p.WebTemplateId == request.Id.Guid);

            var total = query.Count();
            var items = query.ApplyPaginationInput(request).Select(WebTemplateRevisionDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<WebTemplateRevisionDto>>(new ListResultDto<WebTemplateRevisionDto>(items, total));
        }
    }
}
