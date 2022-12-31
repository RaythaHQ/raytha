using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Admins.Queries;

public class GetAdminById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<AdminDto>> 
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<AdminDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<AdminDto> Handle(Query request)
        {                   
            var entity = _db.Users
                .Include(p => p.Roles)
                .FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            return new QueryResponseDto<AdminDto>(AdminDto.GetProjection(entity));
        }
    }
}
