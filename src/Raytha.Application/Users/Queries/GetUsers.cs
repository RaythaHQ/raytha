using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Users.Queries;

public class GetUsers
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<UserDto>>>
    {
        public override string OrderBy { get; init; } = $"LastLoggedInTime {SortOrder.DESCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<UserDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<UserDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.Users.Include(p => p.UserGroups).AsQueryable();

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(d =>
                    d.UserGroups.Any(p => p.Label.Contains(searchQuery))
                    || d.FirstName.ToLower().Contains(searchQuery)
                    || d.LastName.ToLower().Contains(searchQuery)
                    || d.EmailAddress.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync();
            var items = query
                .ApplyPaginationInput(request)
                .Select(UserDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<UserDto>>(
                new ListResultDto<UserDto>(items, total)
            );
        }
    }
}
