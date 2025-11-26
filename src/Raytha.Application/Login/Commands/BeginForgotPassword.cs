using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login.Commands;

public class BeginForgotPassword
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string EmailAddress { get; init; } = null!;
        public bool SendEmail { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        var authScheme = db.AuthenticationSchemes.First(p =>
                            p.DeveloperName
                            == AuthenticationSchemeType.EmailAndPassword.DeveloperName
                        );

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;

        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var emailAddress = request.EmailAddress.ToLower().Trim();
            var entity = _db.Users.FirstOrDefault(p => p.EmailAddress.ToLower() == emailAddress);

            if (entity == null || !entity.IsActive)
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid());
            }

            // Check auth scheme eligibility without revealing to the caller
            var authScheme = _db.AuthenticationSchemes.First(p =>
                p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName
            );

            if (
                (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                || (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
            )
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid());
            }

            ShortGuid guid = ShortGuid.NewGuid();
            var otp = new OneTimePassword
            {
                Id = PasswordUtility.Hash(guid),
                IsUsed = false,
                UserId = entity.Id,
                ExpiresAt = DateTime.UtcNow.AddSeconds(900),
            };

            _db.OneTimePasswords.Add(otp);

            entity.AddDomainEvent(new BeginForgotPasswordEvent(entity, request.SendEmail, guid));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(guid);
        }
    }
}
