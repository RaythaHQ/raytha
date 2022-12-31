using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Users.Queries;

public class GetUserById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<UserDto>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<UserDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<UserDto> Handle(Query request)
        {
            var entity = _db.Users
                .Include(p => p.UserGroups)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("User", request.Id);

            return new QueryResponseDto<UserDto>(UserDto.GetProjection(entity));
        }
    }
}
