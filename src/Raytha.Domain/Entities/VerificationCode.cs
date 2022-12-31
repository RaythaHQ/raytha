namespace Raytha.Domain.Entities;

public class VerificationCode : BaseAuditableEntity
{
    public Guid Code { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Completed { get; set; }
    public string? EmailAddress { get; set; }
    public VerificationCodeType VerificationCodeType { get; set; }
}
