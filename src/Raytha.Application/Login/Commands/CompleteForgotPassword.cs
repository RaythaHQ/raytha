using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.Events;
using Raytha.Application.Common.Utils;
using System.Text.Json.Serialization;

namespace Raytha.Application.Login.Commands;

public class CompleteForgotPassword
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
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH)
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

                if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme is disabled.");
                    return;
                }

                var entity = db.OneTimePasswords
                    .Include(p => p.User)
                    .ThenInclude(p => p.AuthenticationScheme)
                    .FirstOrDefault(p => p.Id == PasswordUtility.Hash(request.Id));

                if (entity == null)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Invalid token.");
                    return;
                }

                if (entity.IsUsed || entity.ExpiresAt < DateTime.UtcNow)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Token is consumed or expired.");
                    return;
                }

                if (!entity.User.IsActive)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "User has been deactivated.");
                    return;
                }

                if (entity.User.IsAdmin && !authScheme.IsEnabledForAdmins)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme disabled for administrators.");
                    return;
                }

                if (!entity.User.IsAdmin && !authScheme.IsEnabledForUsers)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme disabled for public users.");
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
            var entity = _db.OneTimePasswords
            .Include(p => p.User)
            .ThenInclude(p => p.AuthenticationScheme)
            .First(p => p.Id == PasswordUtility.Hash(request.Id));

            var salt = PasswordUtility.RandomSalt();
            entity.User.Salt = salt;
            entity.User.PasswordHash = PasswordUtility.Hash(request.NewPassword, salt);
            entity.IsUsed = true;

            entity.AddDomainEvent(new CompletedForgotPasswordEvent(entity.User, request.SendEmail, request.NewPassword));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.User.Id);
        }
    }
}
