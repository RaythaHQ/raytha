using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.Events;
using Raytha.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace Raytha.Application.Login.Commands;

public class CreateUser
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string FirstName { get; init; } = null!;
        public string LastName { get; init; } = null!;
        public string EmailAddress { get; init; } = null!;

        [JsonIgnore]
        public string Password { get; init; } = null!;

        [JsonIgnore]
        public string ConfirmPassword { get; init; } = null!;
        public bool SendEmail { get; init; } = false;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IRaythaDbContext db)
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(x => x).Custom((request, context) =>
            {
                if (string.IsNullOrEmpty(request.Password) || request.Password.Length < PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH)
                {
                    context.AddFailure("Password", $"Password must be at least {PasswordUtility.PASSWORD_MIN_CHARACTER_LENGTH} characters.");
                    return;
                }

                if (request.Password != request.ConfirmPassword)
                {
                    context.AddFailure("ConfirmPassword", "Confirm Password did not match.");
                    return;
                }

                var emailAndPasswordScheme = db.AuthenticationSchemes.First(p => p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName);
                if (!emailAndPasswordScheme.IsEnabledForUsers)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Email and password authentication scheme must be enabled for users to self register.");
                    return;
                }

                var entity = db.Users
                    .FirstOrDefault(p => p.EmailAddress.ToLower() == request.EmailAddress.ToLower());

                if (entity != null)
                {
                    context.AddFailure("EmailAddress", "Another user with this email address already exists.");
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
            var defaultToEmailAndPasswordScheme = _db.AuthenticationSchemes.First(p => p.DeveloperName == AuthenticationSchemeType.EmailAndPassword.DeveloperName);
            var salt = PasswordUtility.RandomSalt();
            var newUserId = Guid.NewGuid();
            var entity = new User
            {
                Id = newUserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress,
                IsActive = true,
                IsAdmin = false,
                Salt = salt,
                PasswordHash = PasswordUtility.Hash(request.Password, salt),
                SsoId = (ShortGuid)newUserId,
                AuthenticationSchemeId = defaultToEmailAndPasswordScheme.Id
            };

            entity.AddDomainEvent(new UserCreatedEvent(entity, request.SendEmail, request.Password));

            _db.Users.Add(entity);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
