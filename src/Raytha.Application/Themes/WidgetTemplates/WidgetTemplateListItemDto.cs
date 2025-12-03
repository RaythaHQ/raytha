using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates;

public record WidgetTemplateListItemDto : BaseAuditableEntityDto
{
    public required ShortGuid ThemeId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public bool IsBuiltInTemplate { get; init; }

    public static Expression<Func<WidgetTemplate, WidgetTemplateListItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WidgetTemplateListItemDto GetProjection(WidgetTemplate entity)
    {
        if (entity == null)
            return null;

        return new WidgetTemplateListItemDto
        {
            Id = entity.Id,
            ThemeId = entity.ThemeId,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}

