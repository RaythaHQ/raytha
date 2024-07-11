using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates;

public record WebTemplateDto : BaseAuditableEntityDto
{
    public required ShortGuid ThemeId { get; init; }
    public bool IsBaseLayout { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsBuiltInTemplate { get; init; }
    public AuditableUserDto? CreatorUser { get; init; }
    public AuditableUserDto? LastModifierUser { get; init; }
    public ShortGuid? ParentTemplateId { get; init; }
    public WebTemplateDto? ParentTemplate { get; init; }
    public bool AllowAccessForNewContentTypes { get; init; }
    public Dictionary<ShortGuid, string> TemplateAccessToModelDefinitions { get; init; } = null!;

    public static Expression<Func<WebTemplate?, WebTemplateDto?>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebTemplateDto? GetProjection(WebTemplate? entity)
    {
        if (entity == null)
            return null;

        return new WebTemplateDto
        {
            Id = entity.Id,
            ThemeId = entity.ThemeId,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            Content = entity.Content,
            IsBaseLayout = entity.IsBaseLayout,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            CreatorUser = AuditableUserDto.GetProjection(entity.CreatorUser),
            LastModifierUser = AuditableUserDto.GetProjection(entity.LastModifierUser),
            AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes,
            ParentTemplateId = entity.ParentTemplateId,
            ParentTemplate = GetProjection(entity.ParentTemplate),
            TemplateAccessToModelDefinitions = entity.TemplateAccessToModelDefinitions?.ToDictionary(p => (ShortGuid)p.ContentTypeId, p => p.ContentType?.LabelPlural ?? "n/a")!
        };
    }
}
