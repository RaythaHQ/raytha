using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.AuthenticationSchemes;

public record AuthenticationSchemeDto : BaseAuditableEntityDto
{
    public AuthenticationSchemeType AuthenticationSchemeType { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public bool IsBuiltInAuth { get; init; }
    public bool IsEnabledForUsers { get; init; }
    public bool IsEnabledForAdmins { get; init; }
    public string SamlCertificate { get; init; } = string.Empty;
    public string JwtSecretKey { get; init; } = string.Empty;
    public string SignInUrl { get; init; } = string.Empty;
    public string LoginButtonText { get; init; } = string.Empty;
    public string SignOutUrl { get; init; } = string.Empty;
    public AuditableUserDto LastModifierUser { get; init; }

    public int MagicLinkExpiresInSeconds { get; init; }
    public bool JwtUseHighSecurity { get; init; }
    public string SamlIdpEntityId { get; init; } = string.Empty;

    public static Expression<Func<AuthenticationScheme, AuthenticationSchemeDto>> GetProjection()
    {
        return authScheme => GetProjection(authScheme);
    }

    public static AuthenticationSchemeDto GetProjection(AuthenticationScheme entity)
    {
        return new AuthenticationSchemeDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            JwtSecretKey = entity.JwtSecretKey,
            SamlCertificate = entity.SamlCertificate,
            SignInUrl = entity.SignInUrl,
            SignOutUrl = entity.SignOutUrl,
            LoginButtonText = entity.LoginButtonText,
            IsBuiltInAuth = entity.IsBuiltInAuth,
            IsEnabledForUsers = entity.IsEnabledForUsers,
            IsEnabledForAdmins = entity.IsEnabledForAdmins,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            AuthenticationSchemeType = entity.AuthenticationSchemeType,
            MagicLinkExpiresInSeconds = entity.MagicLinkExpiresInSeconds,
            JwtUseHighSecurity = entity.JwtUseHighSecurity,
            SamlIdpEntityId = entity.SamlIdpEntityId,
        };
    }
}
