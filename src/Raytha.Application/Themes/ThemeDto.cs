using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes;

public record ThemeDto : BaseAuditableEntityDto
{
    public required string Title { get; init; }
    public required string DeveloperName { get; init; }
    public required string Description { get; init; }
    public bool IsExportable { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<Theme, ThemeDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static ThemeDto GetProjection(Theme entity)
    {
        return new ThemeDto
        {
            Id = entity.Id,
            Title = entity.Title,
            DeveloperName = entity.DeveloperName,
            Description = entity.Description,
            IsExportable = entity.IsExportable,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            LastModifierUserId = entity.LastModifierUserId,
            LastModificationTime = entity.LastModificationTime,
        };
    }
}