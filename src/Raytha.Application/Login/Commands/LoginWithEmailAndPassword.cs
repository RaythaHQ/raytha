using System.Text.Json.Serialization;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
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

                        // Cleanup stale failed login attempts (older than 2x window)
                        var cleanupCutoff = DateTime.UtcNow.AddSeconds(
                            -2 * authScheme.BruteForceProtectionWindowInSeconds
                        );
                        var staleAttempts = db
                            .FailedLoginAttempts.Where(f => f.LastFailedAttemptAt < cleanupCutoff)
                            .ToList();
                        if (staleAttempts.Any())
                        {
                            db.DbContext.RemoveRange(staleAttempts);
                            db.DbContext.SaveChanges();
                        }

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

                        // Check brute force lockout before proceeding
                        var failedAttempt =
                            emailAddress != null
                                ? db.FailedLoginAttempts.FirstOrDefault(f =>
                                    f.EmailAddress == emailAddress
                                )
                                : null;
                        var windowStart = DateTime.UtcNow.AddSeconds(
                            -authScheme.BruteForceProtectionWindowInSeconds
                        );

                        if (
                            failedAttempt != null
                            && failedAttempt.FailedAttemptCount
                                >= authScheme.BruteForceProtectionMaxFailedAttempts
                            && failedAttempt.LastFailedAttemptAt >= windowStart
                        )
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Too many failed login attempts. Please try again later."
                            );
                            return;
                        }

                        var entity =
                            emailAddress != null
                                ? db.Users.FirstOrDefault(p =>
                                    p.EmailAddress.ToLower() == emailAddress
                                )
                                : null;

                        if (entity == null)
                        {
                            RecordFailedAttempt(db, emailAddress, failedAttempt, windowStart);
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
                            RecordFailedAttempt(db, emailAddress, failedAttempt, windowStart);
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "Invalid email or password."
                            );
                            return;
                        }
                    }
                );
        }

        private static void RecordFailedAttempt(
            IRaythaDbContext db,
            string? emailAddress,
            FailedLoginAttempt? existing,
            DateTime windowStart
        )
        {
            if (string.IsNullOrEmpty(emailAddress))
                return;

            if (existing == null)
            {
                db.FailedLoginAttempts.Add(
                    new FailedLoginAttempt
                    {
                        Id = Guid.NewGuid(),
                        EmailAddress = emailAddress,
                        FailedAttemptCount = 1,
                        LastFailedAttemptAt = DateTime.UtcNow,
                    }
                );
            }
            else if (existing.LastFailedAttemptAt < windowStart)
            {
                // Window expired, reset count
                existing.FailedAttemptCount = 1;
                existing.LastFailedAttemptAt = DateTime.UtcNow;
            }
            else
            {
                // Within window, increment count
                existing.FailedAttemptCount++;
                existing.LastFailedAttemptAt = DateTime.UtcNow;
            }

            db.DbContext.SaveChanges();
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

            var emailAddress = request.EmailAddress.ToLower().Trim();

            var entity = _db.Users.First(p => p.EmailAddress.ToLower() == emailAddress);

            // Clear failed login attempts on successful login
            var failedAttempt = _db.FailedLoginAttempts.FirstOrDefault(f =>
                f.EmailAddress == emailAddress
            );
            if (failedAttempt != null)
            {
                _db.FailedLoginAttempts.Remove(failedAttempt);
            }

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
