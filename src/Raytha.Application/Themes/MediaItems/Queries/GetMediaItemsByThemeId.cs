using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.MediaItems;

namespace Raytha.Application.Themes.MediaItems.Queries;

public class GetMediaItemsByThemeId
{
    public record Query : IRequest<IQueryResponseDto<IReadOnlyCollection<MediaItemDto>>>
    {
        public required ShortGuid ThemeId { get; set; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IReadOnlyCollection<MediaItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<IReadOnlyCollection<MediaItemDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var mediaItems = await _db.ThemeAccessToMediaItems
                .Where(tmi => tmi.ThemeId == request.ThemeId.Guid)
                .Select(tmi => tmi.MediaItem)
                .ToArrayAsync(cancellationToken);

            return new QueryResponseDto<IReadOnlyCollection<MediaItemDto>>(mediaItems.Select(MediaItemDto.GetProjection).ToArray());
        }
    }
}