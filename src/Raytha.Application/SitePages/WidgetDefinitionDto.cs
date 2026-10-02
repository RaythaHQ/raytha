using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages;

/// <summary>
/// A widget type a site page can use: one widget template of the active theme and its settings form.
/// </summary>
public record WidgetDefinitionDto
{
    private const string CustomWidgetIconClass = "bi-puzzle";

    public string DeveloperName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconClass { get; init; } = string.Empty;
    public bool IsBuiltInTemplate { get; init; }
    public IReadOnlyList<FieldDefinition> Fields { get; init; } = [];

    public static WidgetDefinitionDto GetProjection(WidgetTemplate entity)
    {
        var builtIn = BuiltInWidgetType.Find(entity.DeveloperName);
        return new WidgetDefinitionDto
        {
            DeveloperName = entity.DeveloperName ?? string.Empty,
            DisplayName = entity.Label ?? builtIn?.DisplayName ?? entity.DeveloperName ?? string.Empty,
            Description = builtIn?.Description ?? string.Empty,
            IconClass = builtIn?.IconClass ?? CustomWidgetIconClass,
            IsBuiltInTemplate = entity.IsBuiltInTemplate,
            Fields = entity.Fields,
        };
    }
}
