using System.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Roles.Queries;

public class GetRoles
{
    public record Query
        : GetPagedEntitiesInputDto,
            IRequest<IQueryResponseDto<ListResultDto<RoleDto>>>
    {
        public override string OrderBy { get; init; } = $"Label {SortOrder.ASCENDING}";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<RoleDto>>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<ListResultDto<RoleDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db
                .Roles.Include(p => p.ContentTypeRolePermissions)
                .ThenInclude(p => p.ContentType)
                .AsQueryable();

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
                .Select(RoleDto.GetProjection())
                .ToArray();

            return new QueryResponseDto<ListResultDto<RoleDto>>(
                new ListResultDto<RoleDto>(items, total)
            );
        }
    }
}
