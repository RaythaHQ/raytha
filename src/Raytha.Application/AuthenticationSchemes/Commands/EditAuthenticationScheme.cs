using CSharpVitamins;
using FluentValidation;
using MediatR;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.Common.Models;
using Raytha.Application.Common.Utils;
using Raytha.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace Raytha.Application.AuthenticationSchemes.Commands;

public class EditAuthenticationScheme
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;

        [JsonIgnore]
        public string JwtSecretKey { get; init; } = null!;

        [JsonIgnore]
        public string SamlCertificate { get; init; } = null!;
        public string SignInUrl { get; init; } = null!;
        public string SignOutUrl { get; init; } = null!;
        public string LoginButtonText { get; init; } = null!;
        public bool IsEnabledForUsers { get; init; }
        public bool IsEnabledForAdmins { get; init; }
        public int MagicLinkExpiresInSeconds { get; init; }
        public bool JwtUseHighSecurity { get; init; }

        [JsonIgnore]
        public string SamlIdpEntityId { get; init; } = null!;
        public string AuthenticationSchemeType { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command> 
    {
        public Validator(IRaythaDbContext db) 
        {
            RuleFor(x => x.Label).NotEmpty();
            RuleFor(x => x.LoginButtonText).NotEmpty();
            RuleFor(x => x.SignInUrl)
                .NotEmpty()
                .Must(StringExtensions.IsValidUriFormat)
                .When(p => p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt.DeveloperName || p.AuthenticationSchemeType == AuthenticationSchemeType.Saml.DeveloperName)
                .WithMessage("Sign in url is not a valid url.");
            RuleFor(x => x.SignOutUrl)
                .Must(StringExtensions.IsValidUriFormat)
                .When(p => !string.IsNullOrEmpty(p.SignOutUrl))
                .WithMessage("Sign out url is not a valid url.");
            RuleFor(x => x.JwtSecretKey)
                .NotEmpty()
                .When(p => p.AuthenticationSchemeType == AuthenticationSchemeType.Jwt.DeveloperName);
            RuleFor(x => x.SamlCertificate)
                .NotEmpty()
                .When(p => p.AuthenticationSchemeType == AuthenticationSchemeType.Saml.DeveloperName);
            RuleFor(x => x.MagicLinkExpiresInSeconds)
                .NotEmpty()
                .GreaterThanOrEqualTo(30)
                .LessThanOrEqualTo(604800)
                .When(p => p.AuthenticationSchemeType == AuthenticationSchemeType.MagicLink.DeveloperName);
            RuleFor(x => x).Custom((request, context) =>
            {
                var entity = db.AuthenticationSchemes.FirstOrDefault(p => p.Id == request.Id.Guid);
                if (entity == null)
                    throw new NotFoundException("Authentication Scheme", request.Id);

                var onlyOneAdminAuthLeft = db.AuthenticationSchemes.Count(p => p.IsEnabledForAdmins) == 1;
                if (!request.IsEnabledForAdmins && entity.IsEnabledForAdmins && onlyOneAdminAuthLeft)
                {
                    context.AddFailure("IsEnabledForAdmins", "You must have at least 1 authentication scheme enabled for administrators.");
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
            var entity = _db.AuthenticationSchemes
                    .First(p => p.Id == request.Id.Guid);
            
            entity.Label = request.Label;
            entity.SignInUrl = request.SignInUrl;
            entity.SignOutUrl = request.SignOutUrl;
            entity.LoginButtonText = request.LoginButtonText;
            entity.IsEnabledForUsers = request.IsEnabledForUsers;
            entity.IsEnabledForAdmins = request.IsEnabledForAdmins;
            entity.SamlCertificate = request.SamlCertificate;
            entity.JwtSecretKey = request.JwtSecretKey;
            entity.MagicLinkExpiresInSeconds = request.MagicLinkExpiresInSeconds;
            entity.JwtUseHighSecurity = request.JwtUseHighSecurity;
            entity.SamlIdpEntityId = request.SamlIdpEntityId;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
