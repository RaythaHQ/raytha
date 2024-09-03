using System.Linq.Expressions;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WebTemplates;

public class WebTemplateJson
{
    public bool IsBaseLayout { get; init; }
    public required string Label { get; init; }
    public required string DeveloperName { get; init; }
    public required string Content { get; init; }
    public bool IsBuiltInTemplate { get; init; }
    public string? ParentTemplateDeveloperName { get; init; }
    public bool AllowAccessForNewContentTypes { get; init; }

    public static Expression<Func<WebTemplate, WebTemplateJson>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WebTemplateJson GetProjection(WebTemplate entity)
    {
        return new WebTemplateJson
        {
            IsBaseLayout = entity.IsBaseLayout,
            AllowAccessForNewContentTypes = entity.AllowAccessForNewContentTypes,
            Label = entity.Label!,
            Content = entity.Content!,
            DeveloperName = entity.DeveloperName!,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            ParentTemplateDeveloperName = entity.ParentTemplate?.DeveloperName,
        };
    }
}