using System.Linq.Expressions;
using Raytha.Domain.Entities;

namespace Raytha.Application.Themes.WidgetTemplates;

public class WidgetTemplateJson
{
    public required string Label { get; init; }
    public required string DeveloperName { get; init; }
    public required string Content { get; init; }
    public bool IsBuiltInTemplate { get; init; }

    public static Expression<Func<WidgetTemplate, WidgetTemplateJson>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static WidgetTemplateJson GetProjection(WidgetTemplate entity)
    {
        return new WidgetTemplateJson
        {
            Label = entity.Label!,
            Content = entity.Content!,
            DeveloperName = entity.DeveloperName!,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
        };
    }
}

