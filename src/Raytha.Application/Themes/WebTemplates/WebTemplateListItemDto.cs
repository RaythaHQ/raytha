using System.Linq.Expressions;
using CSharpVitamins;
using Raytha.Application.Common.Models;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates;

public record WebTemplateListItemDto : BaseAuditableEntityDto
{
    public bool IsBaseLayout { get; init; }
    public string Label { get; init; } = string.Empty;
    public string DeveloperName { get; init; } = string.Empty;
    public bool IsBuiltInTemplate { get; init; }
    public ShortGuid? ParentTemplateId { get; init; }
    public bool AllowAccessForNewContentTypes { get; init; }

    public static Expression<Func<WebTemplate, WebTemplateListItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebTemplateListItemDto GetProjection(WebTemplate entity)
    {
        if (entity == null)
            return null;

        return new WebTemplateListItemDto
        {
            Id = entity.Id,
            Label = entity.Label,
            DeveloperName = entity.DeveloperName,
            IsBaseLayout = entity.IsBaseLayout,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            CreatorUserId = entity.CreatorUserId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            LastModifierUserId = entity.LastModifierUserId,
            AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes,
            ParentTemplateId = entity.ParentTemplateId,
        };
    }
}
