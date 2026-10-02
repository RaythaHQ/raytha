using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;

namespace Raytha.Application.Login.Queries;

public class GetUserForAuthenticationById
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<LoginDto>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<LoginDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var login = _db.FindLogin(request.Id.Guid);

            if (login == null)
                throw new NotFoundException("User", request.Id);

            return new QueryResponseDto<LoginDto>(login);
        }
    }
}
