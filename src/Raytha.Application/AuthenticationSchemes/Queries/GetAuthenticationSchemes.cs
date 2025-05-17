using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.AuthenticationSchemes.Queries;

public class GetAuthenticationSchemes
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<AuthenticationSchemeDto>>>
    {
        public bool? IsEnabledForAdmins { get; init; }
        public bool? IsEnabledForUsers { get; init; }
        public override string OrderBy { get; init; } = $"Label {SortOrder.Ascending}";
    }

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<ListResultDto<AuthenticationSchemeDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<AuthenticationSchemeDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.AuthenticationSchemes.Include(p => p.LastModifierUser).AsQueryable();

            if (request.IsEnabledForUsers.HasValue)
                query = query.Where(p => p.IsEnabledForUsers);

            if (request.IsEnabledForAdmins.HasValue)
                query = query.Where(p => p.IsEnabledForAdmins);

            if (!string.IsNullOrEmpty(request.Search))
            {
                var searchQuery = request.Search.ToLower();
                query = query.Where(d =>
                    d.Label.ToLower().Contains(searchQuery)
                    || d.DeveloperName.ToLower().Contains(searchQuery)
                );
            }

            var total = await query.CountAsync();
            var items = query
                .ApplyPaginationInput(request)
                .Select(AuthenticationSchemeDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<AuthenticationSchemeDto>>(
                new ListResultDto<AuthenticationSchemeDto>(items, total)
            );
        }
    }
}
