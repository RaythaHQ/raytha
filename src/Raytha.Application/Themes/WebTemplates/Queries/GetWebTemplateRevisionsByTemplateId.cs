using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateRevisionsByTemplateId
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<WebTemplateRevisionDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.WebTemplateRevisions
                .Include(p => p.WebTemplate)
                .Include(p => p.CreatorUser)
                .Where(p => p.WebTemplateId == request.Id.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.ApplyPaginationInput(request).Select(WebTemplateRevisionDto.GetProjection()).ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WebTemplateRevisionDto>>(new ListResultDto<WebTemplateRevisionDto>(items, total));
        }
    }
}
