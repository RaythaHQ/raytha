using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.Queries;

public class GetWebTemplateContentItemRelationsByContentTypeId
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<WebTemplateContentItemRelationDto>>>
    {
        public required ShortGuid ThemeId { get; init; }
        public required ShortGuid ContentTypeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<WebTemplateContentItemRelationDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<IReadOnlyCollection<WebTemplateContentItemRelationDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var webTemplateContentItemRelations = await _db.WebTemplateContentItemRelations
                .Include(wtr => wtr.WebTemplate)
                .Where(wtr => wtr.ContentItem!.ContentTypeId == request.ContentTypeId.Guid && wtr.WebTemplate!.ThemeId == request.ThemeId.Guid)
                .Select(WebTemplateContentItemRelationDto.GetProjection())
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyCollection<WebTemplateContentItemRelationDto>>(webTemplateContentItemRelations);
        }
    }
}