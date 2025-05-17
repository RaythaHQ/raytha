using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Admins.Queries;

public class GetAdminById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<AdminDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<AdminDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<IQueryResponseDto<AdminDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entity = _db
                .Users.Include(p => p.Roles)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            return new QueryResponseDto<AdminDto>(AdminDto.GetProjection(entity));
        }
    }
}
