using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Login.Commands;

public class LoginWithApiKey
{
    public record Command : IRequest<CommandResponseDto<LoginDto>>
    {
        public string ApiKey { get; init; } = null!;
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<LoginDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var hashedApiKey = PasswordUtility.Hash(request.ApiKey);
            var entity = _db
                .ApiKeys.Include(p => p.User)
                .ThenInclude(p => p.UserGroups)
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .FirstOrDefault(p => p.ApiKeyHash == hashedApiKey);

            if (entity == null)
                throw new InvalidApiKeyException("Api key was not found.");

            if (!entity.User.IsActive || !entity.User.IsAdmin)
                throw new InvalidApiKeyException(
                    "Api key is not connected to an active administrator"
                );

            return new CommandResponseDto<LoginDto>(LoginDto.GetProjection(entity.User));
        }
    }
}
