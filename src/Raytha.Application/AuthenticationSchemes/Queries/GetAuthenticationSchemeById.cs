using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.AuthenticationSchemes.Queries;

public class GetAuthenticationSchemeById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<AuthenticationSchemeDto>> 
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<AuthenticationSchemeDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<AuthenticationSchemeDto> Handle(Query request)
        {
            var entity = _db.AuthenticationSchemes.FirstOrDefault(p => p.Id == request.Id.Guid);

            if (entity == null)
                throw new NotFoundException("Authentication Scheme", request.Id);

            return new QueryResponseDto<AuthenticationSchemeDto>(AuthenticationSchemeDto.GetProjection(entity));
        }
    }
}