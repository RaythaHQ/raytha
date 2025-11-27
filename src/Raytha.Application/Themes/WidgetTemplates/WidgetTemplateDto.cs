using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates;

public record WidgetTemplateDto : BaseAuditableEntityDto
{
    public required ShortGuid ThemeId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsBuiltInTemplate { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }

    public static Expression<Func<WidgetTemplate?, WidgetTemplateDto?>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WidgetTemplateDto? GetProjection(WidgetTemplate? entity)
    {
        if (entity == null)
            return null;

        return new WidgetTemplateDto
        {
            Id = entity.Id,
            ThemeId = entity.ThemeId,
            Label = entity.Label ?? string.Empty,
            DeveloperName = entity.DeveloperName ?? string.Empty,
            Content = entity.Content ?? string.Empty,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
        };
    }
}

