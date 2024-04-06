using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Templates.Web.Queries;

public class GetWebTemplates
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
        public bool BaseLayoutsOnly { get; init; } = false;
        public ShortGuid? ContentTypeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<WebTemplateDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.WebTemplates
                .Include(p => p.TemplateAccessToModelDefinitions)
                .ThenInclude(p => p.ContentType)
                .Include(p => p.LastModifierUser)
                .Include(p => p.ParentTemplate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query
                    .Where(d =>
                        (d.Label.ToLower().Contains(searchQuery) ||
                        d.DeveloperName.ToLower().Contains(searchQuery) ||
                        d.LastModifierUser.FirstName.ToLower().Contains(searchQuery) ||
                        d.LastModifierUser.LastName.ToLower().Contains(searchQuery)));
            }

            if (request.ContentTypeId.HasValue)
            {
                query = query
                    .Where(p => p.TemplateAccessToModelDefinitions.Any(c => c.ContentTypeId == request.ContentTypeId.Value.Guid));
            }

            if (request.BaseLayoutsOnly)
                query = query.Where(p => p.IsBaseLayout);

            var total = await query.CountAsync();
            var items = query.ApplyPaginationInput(request).Select(WebTemplateDto.GetProjection()).ToArray();

            return new QueryResponseDto<ListResultDto<WebTemplateDto>>(new ListResultDto<WebTemplateDto>(items, total));
        }
    }
}
