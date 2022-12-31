using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Models;
using CSharpVitamins;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.Events;
using Raytha.Application.Common.Utils;
using Raytha.Application.Common.Exceptions;
using System.Text.Json.Serialization;

namespace Raytha.Application.Login.Commands;

public class ChangePassword
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        [JsonIgnore]
        public string CurrentPassword { get; init; } = null!;

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
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    context.AddFailure("CurrentPassword", $"Must provide a current password.");
                    return;
                }

                if (request.NewPassword.Length < PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH)
                {
                    context.AddFailure("NewPassword", $"Password must be at least {PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH} characters.");
                    return;
                }

                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    context.AddFailure("ConfirmNewPassword", "Confirm Password did not match.");
                    return;
                }

                var authScheme = db.AuthenticationSchemes.First(p =>
                    p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName);

                var entity = db.Users
                    .Include(p => p.AuthenticationScheme)
                    .FirstOrDefault(p => p.Id == request.Id.Guid);

                if (entity == null)
                    throw new NotFoundException("User", request.Id);

                if (!entity.IsActive)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "User has been deactivated.");
                    return;
                }

                if (!authScheme.IsEnabledForAdmins && entity.IsAdmin)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme is disabled for administrators.");
                    return;
                }

                if (!authScheme.IsEnabledForUsers && !entity.IsAdmin)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme is disabled for users.");
                    return;
                }

                var passwordToCheck = PasswordUtility.Hash(request.CurrentPassword, entity.Salt);
                var passwordsMatch = PasswordUtility.IsMatch(entity.PasswordHash, passwordToCheck);
                if (!passwordsMatch)
                {
                    context.AddFailure("CurrentPassword", "Invalid current password.");
                    return;
                }
            });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }

        public async Task<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var entity = _db.Users
                .Include(p => p.AuthenticationScheme)
                .First(p => p.Id == request.Id.Guid);

            var salt = PasswordUtility.RandomSalt();
            entity.Salt = salt;
            entity.PasswordHash = PasswordUtility.Hash(request.NewPassword, salt);

            entity.AddDomainEvent(new AdminPasswordChangedEvent(entity, request.SendEmail));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}