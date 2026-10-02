using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login.Commands;

public class CompleteLoginWithMagicLink
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        public string EmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string Code { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (
                            string.IsNullOrWhiteSpace(request.EmailAddress)
                            || string.IsNullOrWhiteSpace(request.Code)
                        )
                        {
                            return;
                        }

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
                            return;
                        }

                        var emailAddress = request.EmailAddress.ToLower().Trim();
                        var user = db.Users.FirstOrDefault(p =>
                            p.EmailAddress.ToLower() == emailAddress
                        );

                        if (user == null)
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, "Invalid code.");
                            return;
                        }

                        var otpId = MagicLinkCode.OtpId(
                            user.Id,
                            MagicLinkCode.Normalize(request.Code)
                        );
                        var entity = db.OneTimePasswords.FirstOrDefault(p => p.Id == otpId);

                        if (entity == null)
                        {
                            context.AddFailure(Constants.VALIDATION_SUMMARY, "Invalid code.");
                            return;
                        }

                        if (entity.IsUsed || entity.ExpiresAt < DateTime.UtcNow)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Code is consumed or expired."
                            );
                            return;
                        }

                        if (!user.IsActive)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "User has been deactivated."
                            );
                            return;
                        }

                        if (user.IsAdmin && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for administrators."
                            );
                            return;
                        }

                        if (!user.IsAdmin && !authScheme.IsEnabledForUsers)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for public users."
                            );
                            return;
                        }
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

        public async ValueTask<CommandResponseDto<LoginDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var authScheme = _db.AuthenticationSchemes.First(p =>
                p.AuthenticationSchemeType
                == AuthenticationSchemeType.EmailAndPassword.DeveloperName
            );

            var emailAddress = request.EmailAddress.ToLower().Trim();
            var user = _db.Users.First(p => p.EmailAddress.ToLower() == emailAddress);

            var otpId = MagicLinkCode.OtpId(user.Id, MagicLinkCode.Normalize(request.Code));
            var entity = _db.OneTimePasswords.First(p => p.Id == otpId);

            entity.IsUsed = true;
            user.LastLoggedInTime = DateTime.UtcNow;
            user.AuthenticationSchemeId = authScheme.Id;
            user.SsoId = (ShortGuid)user.Id;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<LoginDto>(
                new LoginDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    EmailAddress = user.EmailAddress,
                    LastModificationTime = user.LastModificationTime,
                    AuthenticationScheme = authScheme.DeveloperName,
                    SsoId = user.SsoId,
                    IsAdmin = user.IsAdmin,
                }
            );
        }
    }
}
