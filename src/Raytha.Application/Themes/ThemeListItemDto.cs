using System.Linq.Expressions;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes;

public record ThemeListItemDto : BaseAuditableEntityDto
{
    public required string Title { get; init; }
    public required string DeveloperName { get; init; }
    public required string Description { get; init; }
    public bool IsExportable { get; init; }
    public bool IsActive { get; init; }

    public static Expression<Func<Theme, ThemeListItemDto>> GetProjection(Guid activeThemeId)
    {
        return entity => GetProjection(entity, activeThemeId);
    }

    public static ThemeListItemDto GetProjection(Theme entity, Guid activeThemeId)
    {
        return new ThemeListItemDto
        {
            Id = entity.Id,
            Title = entity.Title,
            DeveloperName = entity.DeveloperName,
            Description = entity.Description,
            IsExportable = entity.IsExportable,
            IsActive = entity.Id == activeThemeId,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModifierUserId = entity.LastModifierUserId,
            LastModificationTime = entity.LastModificationTime,
        };
    }
}

