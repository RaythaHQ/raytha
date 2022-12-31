using MediatR;
using Microsoft.EntityFrameworkCore;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Login.Queries;

public class GetForgotPasswordTokenValidity
{
    public record Query : GetEntityByIdInputDto, IRequest<IQueryResponseDto<bool>>
    {
    }

    public class Handler : RequestHandler<Query, IQueryResponseDto<bool>>
    {
        private readonly IRaythaDbContext _db;
        public Handler(IRaythaDbContext db)
        {
            _db = db;
        }
        protected override IQueryResponseDto<bool> Handle(Query request)
        {
            var authScheme = _db.AuthenticationSchemes.First(p =>
                    p.AuthenticationSchemeType == AuthenticationSchemeType.EmailAndPassword);

            if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Authentication scheme is disabled");

            var entity = _db.OneTimePasswords
                .Include(p => p.User)
                .ThenInclude(p => p.AuthenticationScheme)
                .FirstOrDefault(p => p.Id == PasswordUtility.Hash(request.Id));

            if (entity == null)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Invalid token.");

            if (entity.IsUsed || entity.ExpiresAt < DateTime.UtcNow)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Token is consumed or expired.");

            if (!entity.User.IsActive)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "User has been deactivated.");

            if (entity.User.IsAdmin && !authScheme.IsEnabledForAdmins)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Authentication scheme disabled for administrators.");

            if (!entity.User.IsAdmin && !authScheme.IsEnabledForUsers)
                return new QueryResponseDto<bool>(Constants.VALIDATION_SUMMARY, "Authentication scheme disabled for public users.");

            return new QueryResponseDto<bool>(true);
        }
    }
}
