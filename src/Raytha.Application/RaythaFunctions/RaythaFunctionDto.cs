using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.RaythaFunctions;

public record RaythaFunctionDto : BaseAuditableEntityDto
{
    public required string Name { get; init; }
    public required string DeveloperName { get; init; }
    public required RaythaFunctionTriggerType TriggerType { get; init; }
    public bool IsActive { get; init; }
    public required string Code { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<RaythaFunction, RaythaFunctionDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static RaythaFunctionDto GetProjection(RaythaFunction entity)
    {
        return new RaythaFunctionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DeveloperName = entity.DeveloperName,
            TriggerType = entity.TriggerType,
            IsActive = entity.IsActive,
            Code = entity.Code,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
        };
    }
}
