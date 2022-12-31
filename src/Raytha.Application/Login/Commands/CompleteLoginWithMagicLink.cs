using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.ValueObjects;
using Raytha.Application.Common.Utils;
using CSharpVitamins;

namespace Raytha.Application.Login.Commands;

public class CompleteLoginWithMagicLink
{
    public record Command : LoggableEntityRequest<CommandResponseDto<LoginDto>> 
    {
    }

    public class Validator : AbstractValidator<Command> 
    {
        public Validator(IRaythaDbContext db) 
        {
            RuleFor(x => x).Custom((request, context) =>
            {
                var authScheme = db.AuthenticationSchemes.First(p =>
                    p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName);

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

    public class Handler : IRequestHandler<Command, CommandResponseDto<LoginDto>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        public async Task<CommandResponseDto<LoginDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var authScheme = _db.AuthenticationSchemes.First(p => 
                    p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword.DeveloperName);

            var entity = _db.OneTimePasswords
                .Include(p => p.User)
                .ThenInclude(p => p.AuthenticationScheme)
                .First(p => p.Id == PasswordUtility.Hash(request.Id));

            entity.IsUsed = true;
            entity.User.LastLoggedInTime = DateTime.UtcNow;
            entity.User.AuthenticationSchemeId = authScheme.Id;
            entity.User.SsoId = (ShortGuid)entity.UserId;

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<LoginDto>(new LoginDto
            {
                Id = entity.User.Id,
                FirstName = entity.User.FirstName,
                LastName = entity.User.LastName,
                EmailAddress = entity.User.EmailAddress,
                LastModificationTime = entity.User.LastModificationTime
            });
        }
    }
}
