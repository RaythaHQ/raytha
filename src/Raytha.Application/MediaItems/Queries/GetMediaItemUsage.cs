using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.MediaItems.Queries;

/// <summary>
/// Themes that own a file and web templates whose source mentions its object key, so the admin
/// can see what breaks before deleting it. Content items are not scanned.
/// </summary>
public class GetMediaItemUsage
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<MediaItemUsageDto>> { }

    public record MediaItemUsageDto
    {
        public IReadOnlyList<ThemeUsage> Themes { get; init; } = [];
        public IReadOnlyList<WebTemplateUsage> WebTemplates { get; init; } = [];
    }

    public record ThemeUsage(ShortGuid Id, string Title);

    public record WebTemplateUsage(ShortGuid Id, string Label, ShortGuid ThemeId, string ThemeTitle);

    public class Handler : IRequestHandler<Query, IQueryResponseDto<MediaItemUsageDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<MediaItemUsageDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var objectKey = await _db
                .MediaItems.AsNoTracking()
                .Where(p => p.Id == request.Id.Guid)
                .Select(p => p.ObjectKey)
                .FirstOrDefaultAsync(cancellationToken);
            if (objectKey == null)
                throw new NotFoundException("Media item", request.Id);

            var themes = await _db
                .ThemeAccessToMediaItems.AsNoTracking()
                .Where(p => p.MediaItemId == request.Id.Guid)
                .OrderBy(p => p.Theme!.Title)
                .Select(p => new { p.ThemeId, p.Theme!.Title })
                .ToListAsync(cancellationToken);

            var templates = await _db
                .WebTemplates.AsNoTracking()
                .Where(p => p.Content != null && p.Content.Contains(objectKey))
                .OrderBy(p => p.Theme!.Title)
                .ThenBy(p => p.Label)
                .Select(p => new
                {
                    p.Id,
                    p.Label,
                    p.ThemeId,
                    ThemeTitle = p.Theme!.Title,
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<MediaItemUsageDto>(
                new MediaItemUsageDto
                {
                    Themes = themes.Select(t => new ThemeUsage(t.ThemeId, t.Title)).ToList(),
                    WebTemplates = templates
                        .Select(t => new WebTemplateUsage(t.Id, t.Label ?? "", t.ThemeId, t.ThemeTitle))
                        .ToList(),
                }
            );
        }
    }
}
