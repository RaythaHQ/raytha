using System.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.MediaItems.Queries;

public class GetMediaItems
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<MediaItemDto>>>
    {
        public override string OrderBy { get; init; } = $"CreationTime {SortOrder.DESCENDING}";
        public string? ContentType { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<MediaItemDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<MediaItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.MediaItems.AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(p => p.ObjectKey.ToLower().Contains(searchQuery));
            }

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                var contentType = request.ContentType.ToLower();
                query = query.Where(mi => mi.ContentType.ToLower().Contains(contentType));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = query
                .ApplyPaginationInput(request)
                .Select(MediaItemDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<MediaItemDto>>(
                new ListResultDto<MediaItemDto>(items, total)
            );
        }
    }
}
