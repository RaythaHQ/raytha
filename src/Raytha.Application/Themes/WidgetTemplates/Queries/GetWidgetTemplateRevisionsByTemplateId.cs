using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates.Queries;

public class GetWidgetTemplateRevisionsByTemplateId
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WidgetTemplateRevisionDto>>>
    {
        public ShortGuid Id { get; init; }
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.Descending}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WidgetTemplateRevisionDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WidgetTemplateRevisionDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .WidgetTemplateRevisions.Include(p => p.WidgetTemplate)
                .Include(p => p.CreatorUser)
                .Where(p => p.WidgetTemplateId == request.Id.Guid);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .ApplyPaginationInput(request)
                .Select(WidgetTemplateRevisionDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<ListResultDto<WidgetTemplateRevisionDto>>(
                new ListResultDto<WidgetTemplateRevisionDto>(items, total)
            );
        }
    }
}

