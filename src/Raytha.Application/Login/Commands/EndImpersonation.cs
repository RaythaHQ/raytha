using CSharpVitamins;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;

namespace Raytha.Application.Login.Commands;

public class EndImpersonation
{
    /// <summary>Both ids come from the signed-in principal, so the audit row names both parties.</summary>
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        public ShortGuid ImpersonatorId { get; init; }
        public ShortGuid TargetUserId { get; init; }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public ValueTask<CommandResponseDto<LoginDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var impersonator = _db.FindLogin(request.ImpersonatorId.Guid);
            return ValueTask.FromResult(
                impersonator is { IsActive: true, IsAdmin: true }
                    ? new CommandResponseDto<LoginDto>(impersonator)
                    : new CommandResponseDto<LoginDto>(
                        Constants.VALIDATION_SUMMARY,
                        "The administrator who started this session can no longer sign in. You have been signed out."
                    )
            );
        }
    }
}
