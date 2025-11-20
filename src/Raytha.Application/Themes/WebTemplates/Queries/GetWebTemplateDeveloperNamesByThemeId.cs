using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplateDeveloperNamesByThemeId
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<string>>>
    {
        public required ShortGuid ThemeId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<string>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IReadOnlyCollection<string>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var webTemplateDeveloperNameItems = await _db
                .WebTemplates.Where(wt => wt.ThemeId == request.ThemeId.Guid)
                .Select(wt => wt.DeveloperName!)
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyCollection<string>>(webTemplateDeveloperNameItems);
        }
    }
}
