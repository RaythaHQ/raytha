using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Users.Queries;

public class GetUserById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<UserDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<UserDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<UserDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db
                .Users.AsNoTracking()
                .Include(p => p.UserGroups)
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                throw new NotFoundException("User", request.Id);

            return new QueryResponseDto<UserDto>(UserDto.GetProjection(entity));
        }
    }
}
