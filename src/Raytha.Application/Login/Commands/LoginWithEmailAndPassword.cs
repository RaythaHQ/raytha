using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login.Commands;

public class LoginWithEmailAndPassword
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        public string EmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string Password { get; init; } = null!;
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
                            p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
                        );

                        if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme is disabled."
                            );
                            return;
                        }

                        if (string.IsNullOrEmpty(request.Password))
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Password is required."
                            );
                            return;
                        }

                        var emailAddress = !string.IsNullOrWhiteSpace(request.EmailAddress)
                            ? request.EmailAddress.ToLower().Trim()
                            : null;
                        var entity =
                            emailAddress != null
                                ? db.Users.FirstOrDefault(p =>
                                    p.EmailAddress.ToLower() == emailAddress
                                )
                                : null;

                        if (entity == null)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Invalid email or password."
                            );
                            return;
                        }

                        if (!entity.IsActive)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Your account has been deactivated."
                            );
                            return;
                        }

                        if (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for administrators."
                            );
                            return;
                        }

                        if (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled for public users."
                            );
                            return;
                        }

                        var passwordToCheck = PasswordUtility.Hash(request.Password, entity.Salt);
                        var passwordsMatch = PasswordUtility.IsMatch(
                            entity.PasswordHash,
                            passwordToCheck
                        );
                        if (!passwordsMatch)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Invalid email or password."
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
                p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword
            );

            var entity = _db.Users.First(p =>
                p.EmailAddress.ToLower() == request.EmailAddress.ToLower().Trim()
            );

            entity.LastLoggedInTime = DateTime.UtcNow;
            entity.AuthenticationSchemeId = authScheme.Id;
            entity.SsoId = (ShortGuid)entity.Id;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<LoginDto>(
                new LoginDto
                {
                    Id = entity.Id,
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    EmailAddress = entity.EmailAddress,
                    LastModificationTime = entity.LastModificationTime,
                    AuthenticationScheme = authScheme.DeveloperName,
                    SsoId = entity.SsoId,
                    IsAdmin = entity.IsAdmin,
                }
            );
        }
    }
}
