using System.Text.Json.Serialization;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Security;

namespace Raytha.Application.Login.Commands;

public class BeginImpersonation
{
    /// <summary>
    /// Which population the endpoint serves. Each has its own authorization policy, so a target
    /// of the other kind is reported as not found rather than reached through the wrong route.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TargetKind
    {
        WebsiteUser,
        Admin,
    }

    public record Command : LoggableEntityRequest<CommandResponseDto<LoginDto>>
    {
        public TargetKind Kind { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db, ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var target = db.FindLogin(request.Id.Guid);
                        if (target is null || target.IsAdmin != (request.Kind == TargetKind.Admin))
                            return;

                        var caller = currentUser.UserId is { } callerId
                            ? db.FindLogin(callerId.Guid)
                            : null;
                        context.AddDenial(
                            ImpersonationRules.Check(
                                caller is null ? null : ImpersonationParty.Of(caller),
                                ImpersonationParty.Of(target),
                                callerIsImpersonating: currentUser.ImpersonatorId is not null
                            )
                        );
                    }
                );
        }
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
            var target = _db.FindLogin(request.Id.Guid);
            if (target is null || target.IsAdmin != (request.Kind == TargetKind.Admin))
                throw new NotFoundException(
                    request.Kind == TargetKind.Admin ? "Admin" : "User",
                    request.Id
                );

            return ValueTask.FromResult(new CommandResponseDto<LoginDto>(target));
        }
    }
}
