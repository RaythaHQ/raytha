using System.Linq.Dynamic.Core;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WebTemplates.Queries;

public class GetWebTemplates
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        public const string DEFAULT_ORDER = $"Label {SortOrder.ASCENDING}";

        public override string OrderBy { get; init; } = DEFAULT_ORDER;
        public bool BaseLayoutsOnly { get; init; } = false;
        public ShortGuid? ContentTypeId { get; init; }
        public ShortGuid? ThemeId { get; init; }

        /// <summary>This admin's favorites sort first and carry <c>IsFavorite</c>.</summary>
        public ShortGuid? CurrentUserId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<WebTemplateDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<WebTemplateDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.WebTemplates.AsNoTracking();

            if (request.ThemeId.HasValue)
            {
                var themeId = request.ThemeId.Value.Guid;
                query = query.Where(wt => wt.ThemeId == themeId);
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(wt =>
                    (wt.Label != null && wt.Label.ToLower().Contains(searchQuery))
                    || (wt.DeveloperName != null && wt.DeveloperName.ToLower().Contains(searchQuery))
                );
            }

            if (request.ContentTypeId is { } contentType && contentType.Guid != Guid.Empty)
            {
                var contentTypeId = contentType.Guid;
                query = query.Where(wt =>
                    wt.TemplateAccessToModelDefinitions.Any(c => c.ContentTypeId == contentTypeId)
                );
            }

            if (request.BaseLayoutsOnly)
            {
                query = query.Where(wt => wt.IsBaseLayout);
            }

            var total = await query.CountAsync(cancellationToken);

            var userId = request.CurrentUserId?.Guid ?? Guid.Empty;
            var requestedOrder = string.Join(
                ", ",
                request
                    .GetOrderByItems()
                    .Where(p => typeof(WebTemplate).GetProperty(p.OrderByPropertyName) != null)
            );
            var ordered = query
                .OrderByDescending(wt => wt.UserFavorites.Any(u => u.Id == userId))
                .ThenBy(requestedOrder.Length > 0 ? requestedOrder : Query.DEFAULT_ORDER)
                .ThenBy(wt => wt.Id);

            var pageSize = Math.Max(request.PageSize, 1);
            var page = await ordered
                .Skip((Math.Max(request.PageNumber, 1) - 1) * pageSize)
                .Take(pageSize)
                .Select(wt => new
                {
                    Template = wt,
                    IsFavorite = wt.UserFavorites.Any(u => u.Id == userId),
                })
                .ToArrayAsync(cancellationToken);

            var items = page.Select(p =>
                    WebTemplateDto.GetProjection(p.Template)! with
                    {
                        IsFavorite = p.IsFavorite,
                    }
                )
                .ToArray();

            return new QueryResponseDto<ListResultDto<WebTemplateDto>>(
                new ListResultDto<WebTemplateDto>(items, total)
            );
        }
    }
}
