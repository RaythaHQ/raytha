using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Users.Commands;

public class ResetPassword
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        [JsonIgnore]
        public string NewPassword { get; init; } = null!;

        [JsonIgnore]
        public string ConfirmNewPassword { get; init; } = null!;
        public bool SendEmail { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (
                            request.NewPassword.Length
                            < PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH
                        )
                        {
                            context.AddFailure(
                                "NewPassword",
                                $"Password must be at least {PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH} characters."
                            );
                            return;
                        }

                        if (request.NewPassword != request.ConfirmNewPassword)
                        {
                            context.AddFailure(
                                "ConfirmNewPassword",
                                "Confirm Password did not match."
                            );
                            return;
                        }

                        var authScheme = db.AuthenticationSchemes.First(p =>
                            p.AuthenticationSchemeType
                            == AuthenticationSchemeType.EmailAndPassword.DeveloperName
                        );

                        if (!authScheme.IsEnabledForUsers)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Authentication scheme disabled."
                            );
                            return;
                        }

                        var entity = db.Users.FirstOrDefault(p => p.Id == request.Id.Guid);

                        if (entity == null)
                            throw new NotFoundException("User", request.Id);

                        if (!entity.IsActive)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "User has been deactivated."
                            );
                            return;
                        }

                        if (entity.IsAdmin)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot reset the password of an active administrator."
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
            var entity = _db.Users.First(p => p.Id == request.Id.Guid);

            var salt = PasswordUtility.RandomSalt();
            entity.Salt = salt;
            entity.PasswordHash = PasswordUtility.Hash(request.NewPassword, salt);

            entity.AddDomainEvent(
                new UserPasswordResetEvent(entity, request.SendEmail, request.NewPassword)
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
