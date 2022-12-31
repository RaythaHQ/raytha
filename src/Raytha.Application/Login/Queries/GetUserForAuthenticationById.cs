using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Login.Queries;

public class GetUserForAuthenticationById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<LoginDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<LoginDto> Handle(Query request)
        {
            var entity = _db.Users
                .Include(p => p.Roles)
                .ThenInclude(p => p.ContentTypeRolePermissions)
                .ThenInclude(p => p.ContentType)
                .Include(p => p.UserGroups)
                .Include(p => p.AuthenticationScheme)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("User", request.Id);

            return new QueryResponseDto<LoginDto>(LoginDto.GetProjection(entity));
        }
    }
}