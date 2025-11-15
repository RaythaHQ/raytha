using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.RaythaFunctions;

public record RaythaFunctionRevisionDto : BaseAuditableEntityDto
{
    public ShortGuid RaythaFunctionId { get; init; }
    public required string Code { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<
        Func<RaythaFunctionRevision, RaythaFunctionRevisionDto>
    > GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static RaythaFunctionRevisionDto GetProjection(RaythaFunctionRevision entity)
    {
        return new RaythaFunctionRevisionDto
        {
            Id = entity.Id,
            RaythaFunctionId = entity.RaythaFunctionId,
            Code = entity.Code,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
        };
    }
}
