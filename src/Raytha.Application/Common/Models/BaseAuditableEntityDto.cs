using CSharpVitamins;

namespace Raytha.Application.Common.Models;

public record BaseAuditableEntityDto : BaseEntityDto
{
    public DateTime CreationTime { get; init; }
    public ShortGuid? CreatorUserId { get; init; }
    public ShortGuid? LastModifierUserId { get; init; }
    public DateTime? LastModificationTime { get; init; }
}
