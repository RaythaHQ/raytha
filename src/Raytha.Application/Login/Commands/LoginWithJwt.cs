using MediatR;
using FluentValidation;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Application.Common.Utils;
using System.IdentityModel.Tokens.Jwt;
using Raytha.Application.Common.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Raytha.Application.Common.Security;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Raytha.Application.Login.Commands;

public class LoginWithJwt
{
    public record Command : LoggableRequest<CommandResponseDto<LoginDto>>
    {
        [JsonIgnore]
        public string Token { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command> 
    {
        public Validator(IRaythaDbContext db) 
        {
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.DeveloperName).NotEmpty();
            RuleFor(x => x).Custom((request, context) =>
            {
                var developerName = request.DeveloperName.ToDeveloperName();
                var authScheme = db.AuthenticationSchemes.FirstOrDefault(p =>
                    p.AuthenticationSchemeType == developerName);

                if (authScheme == null)
                {
                    throw new NotFoundException("Auth scheme", $"{request.DeveloperName} was not found");
                }

                if (!authScheme.IsEnabledForUsers && !authScheme.IsEnabledForAdmins)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Authentication scheme is disabled");
                    return;
                }

                JwtPayload payload;
                try
                {             
                    var validationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authScheme.JwtSecretKey.PadRight((256 / 8), '\0'))),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ClockSkew = TimeSpan.Zero
                    };
                    SecurityToken decodedToken;
                    new JwtSecurityTokenHandler().ValidateToken(request.Token, validationParameters, out decodedToken);

                    payload = ((JwtSecurityToken)decodedToken).Payload;
                }
                catch (SecurityTokenExpiredException)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Security token has expired.");
                    return;
                }
                catch (Exception)
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, "Invalid security token.");
                    return;
                }

                if (authScheme.JwtUseHighSecurity)
                {
                    var jti = payload.Jti;
                    if (string.IsNullOrEmpty(jti))
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, "JWT high security enabled: 'jti' attribute is required in security token.");
                        return;
                    }

                    var jtiResult = db.JwtLogins.FirstOrDefault(p => p.Jti == jti);
                    if (jtiResult != null)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"Security token already consumed: {jtiResult.Jti}");
                        return;
                    }
                }

                string email = payload.GetValueOrDefault<string, object>(JwtRegisteredClaimNames.Email) as string;

                if (!payload.ContainsKey(JwtRegisteredClaimNames.Email))
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, $"'email' is missing from security token");
                    return;
                }

                email = email.ToLower().Trim();

                if (!email.IsValidEmailAddress())
                {
                    context.AddFailure(Constants.VALIDATION_SUMMARY, $"'email' is not a valid email address");
                    return;
                }

                User entity = null;
                var sub = payload.Sub;
                if (payload.ContainsKey(JwtRegisteredClaimNames.Sub))
                {
                    entity = db.Users.FirstOrDefault(p => p.SsoId == sub && p.AuthenticationSchemeId == authScheme.Id);
                }
                else
                {
                    entity = db.Users.FirstOrDefault(p => p.EmailAddress.ToLower() == email);
                }

                if (entity != null)
                {
                    if (entity.IsAdmin && !authScheme.IsEnabledForAdmins)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"Authentication scheme disabled for administrators.");
                        return;
                    }

                    if (!entity.IsAdmin && !authScheme.IsEnabledForUsers)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"Authentication scheme disabled for public users.");
                        return;
                    }

                    if (!entity.IsActive)
                    {
                        context.AddFailure(Constants.VALIDATION_SUMMARY, $"User has been deactivated.");
                        return;
                    }
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
            var authScheme = _db.AuthenticationSchemes.First(p => p.DeveloperName == request.DeveloperName.ToDeveloperName());
            
            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(authScheme.JwtSecretKey.PadRight((256 / 8), '\0'))),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };
            SecurityToken decodedToken;
            var claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(request.Token, validationParameters, out decodedToken);
            var payload = ((JwtSecurityToken)decodedToken).Payload;

            JwtLogin jwtLogin = null;
            if (authScheme.JwtUseHighSecurity)
            {
                var jti = payload.Jti;
                jwtLogin = new JwtLogin
                {
                    Id = Guid.NewGuid(),
                    Jti = jti,
                };
            }

            string sub = payload.Sub;
            string email = payload.GetValueOrDefault<string, object>(JwtRegisteredClaimNames.Email) as string;
            string givenName = payload.GetValueOrDefault<string, object>(JwtRegisteredClaimNames.GivenName) as string;
            string familyName = payload.GetValueOrDefault<string, object>(JwtRegisteredClaimNames.FamilyName) as string;
            IEnumerable<string> userGroups = payload.Claims.Where(p => p.Type == RaythaClaimTypes.UserGroups).Select(p => p.Value);

            User entity = null;

            if (!string.IsNullOrWhiteSpace(sub))
            {
                entity = _db.Users.Include(p => p.UserGroups).FirstOrDefault(p => p.SsoId == sub && p.AuthenticationSchemeId == authScheme.Id);
            }

            if (entity == null)
            {
                var cleanedEmail = email.Trim().ToLower();
                entity = _db.Users.Include(p => p.UserGroups).FirstOrDefault(p => p.EmailAddress.ToLower() == cleanedEmail);
            }

            ICollection<UserGroup> foundUserGroups = null;
            if (userGroups.Any())
            {
                foundUserGroups = _db.UserGroups.Where(p => userGroups.Any(c => c == p.DeveloperName)).ToList();
            }

            //no user found at all, create a new user on the fly
            bool firstTime = false;
            if (entity == null)
            {
                firstTime = true;
                var id = Guid.NewGuid();
                ShortGuid shortGuid = id;
                var salt = PasswordUtility.RandomSalt();
                entity = new User
                {
                    Id = id,
                    EmailAddress = email.Trim(),
                    FirstName = givenName.IfNullOrEmpty("SsoVisitor"),
                    LastName = familyName.IfNullOrEmpty(shortGuid),
                    IsActive = true,
                    Salt = salt,
                    PasswordHash = PasswordUtility.Hash(PasswordUtility.RandomPassword(12), salt),
                    SsoId = sub,
                    UserGroups = foundUserGroups
                };
            }
            else
            {
                entity.SsoId = sub.IfNullOrEmpty(entity.SsoId);
                entity.FirstName = givenName.IfNullOrEmpty(entity.FirstName);
                entity.LastName = familyName.IfNullOrEmpty(entity.LastName);
                entity.EmailAddress = email.Trim();

                if (foundUserGroups != null && foundUserGroups.Any())
                {
                    foreach (var ug in entity.UserGroups)
                    {
                        if (!foundUserGroups.Any(p => p.DeveloperName == ug.DeveloperName))
                        {
                            entity.UserGroups.Remove(ug);
                        }
                    }

                    foreach (var ug in foundUserGroups)
                    {
                        if (!entity.UserGroups.Any(p => p.DeveloperName == ug.DeveloperName))
                        {
                            entity.UserGroups.Add(ug);
                        }
                    }
                }
            }

            entity.LastLoggedInTime = DateTime.UtcNow;
            entity.AuthenticationSchemeId = authScheme.Id;

            if (firstTime)
                _db.Users.Add(entity);
                
            if (jwtLogin != null)
                _db.JwtLogins.Add(jwtLogin);

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<LoginDto>(new LoginDto
            {
                Id = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                EmailAddress = entity.EmailAddress,
                LastModificationTime = entity.LastModificationTime,
                AuthenticationScheme = authScheme.DeveloperName,
                SsoId = entity.SsoId,
                IsAdmin = entity.IsAdmin
            });
        }
    }
}
