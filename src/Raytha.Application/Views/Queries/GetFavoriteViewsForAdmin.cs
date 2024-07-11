using CSharpVitamins;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Views.Queries;

public class GetFavoriteViewsForAdmin
{
    public record Query : GetPagedEntitiesInputDto, IRequest<IQueryResponseDto<ListResultDto<ViewDto>>>
    {
        public ShortGuid? ContentTypeId { get; init; }
        public ShortGuid UserId { get; init; }
        public string ContentTypeDeveloperName { get; init; } = null!;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<ViewDto>>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<ViewDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var query = _db.Views
                .Include(p => p.Route)
                .Include(p => p.ContentType)
                .Include(p => p.LastModifierUser)
                .Include(p => p.UserFavorites)
                .AsQueryable();

            if (request.ContentTypeId.HasValue)
            {
                query = query.Where(p => p.ContentTypeId == request.ContentTypeId.Value.Guid);
            }
            else if (!string.IsNullOrEmpty(request.ContentTypeDeveloperName))
            {
                query = query.Where(p => p.ContentType.DeveloperName == request.ContentTypeDeveloperName.ToDeveloperName());
            }

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

            query = query.Where(p => p.UserFavorites.Any(p => p.Id == request.UserId.Guid));

            var total = await query.CountAsync(cancellationToken);
            var items = query.ApplyPaginationInput(request).Select(v => ViewDto.GetProjection(v)).ToArray();

            return new QueryResponseDto<ListResultDto<ViewDto>>(new ListResultDto<ViewDto>(items, total));
        }
    }
}
