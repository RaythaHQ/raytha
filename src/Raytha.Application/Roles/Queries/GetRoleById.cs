using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Roles.Queries;

public class GetRoleById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<RoleDto>>
    {
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<RoleDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<RoleDto>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entity = _db.Roles
                .Include(p => p.ContentTypeRolePermissions)
                .ThenInclude(p => p.ContentType)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Role", request.Id);

            return new QueryResponseDto<RoleDto>(RoleDto.GetProjection(entity));
        }
    }
}
