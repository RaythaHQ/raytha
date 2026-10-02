using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login.Commands;

public class BeginLoginWithMagicLink
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
                            p.AuthenticationSchemeType
                            == AuthenticationSchemeType.MagicLink.DeveloperName
                        );

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled."
                            );
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
            var authScheme = _db.AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName
            );

            var emailAddress = request.EmailAddress.ToLower().Trim();
            var entity = _db
                .Users.Include(p => p.AuthenticationScheme)
                .FirstOrDefault(p => p.EmailAddress.ToLower() == emailAddress);

            // Answer an ineligible address exactly like an eligible one so the form cannot be
            // used to discover accounts.
            if (
                entity == null
                || !entity.IsActive
                || (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                || (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
            )
            {
                return new CommandResponseDto<ShortGuid>(ShortGuid.NewGuid());
            }

            var code = MagicLinkCode.Generate();
            var otp = new OneTimePassword
            {
                Id = MagicLinkCode.OtpId(entity.Id, code),
                IsUsed = false,
                UserId = entity.Id,
                ExpiresAt = DateTime.UtcNow.AddSeconds(authScheme.MagicLinkExpiresInSeconds),
            };

            _db.OneTimePasswords.Add(otp);

            entity.AddDomainEvent(
                new BeginLoginWithMagicLinkEvent(
                    entity,
                    request.SendEmail,
                    code,
                    authScheme.MagicLinkExpiresInSeconds
                )
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
