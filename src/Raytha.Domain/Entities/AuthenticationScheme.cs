namespace Raytha.Domain.Entities;

public class AuthenticationScheme : BaseAuditableEntity
{
    public bool IsBuiltInAuth { get; set; }
    public bool IsEnabledForUsers { get; set; }
    public bool IsEnabledForAdmins { get; set; }
    public AuthenticationSchemeType? AuthenticationSchemeType { get; set; }
    public string? Label { get; set; }
    public string? DeveloperName { get; set; }
    public int MagicLinkExpiresInSeconds { get; set; }
    public string? SamlCertificate { get; set; }
    public string? SamlIdpEntityId { get; set; }
    public string? JwtSecretKey { get; set; }
    public bool JwtUseHighSecurity { get; set; }
    public string? SignInUrl { get; set; }
    public string? LoginButtonText { get; set; }
    public string? SignOutUrl { get; set; }
}
