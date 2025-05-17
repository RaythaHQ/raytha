using CSharpVitamins;

namespace Raytha.Application.Common.Models;

public record BaseFullAuditableEntityDto : BaseAuditableEntityDto
{
    public ShortGuid? DeleterUserId { get; init; }
    public DateTime? DeletionTime { get; init; }
    public bool IsDeleted { get; init; }
}
