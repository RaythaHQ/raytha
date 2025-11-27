using Raytha.Application.SitePages.Widgets.Definitions;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages.Widgets;

/// <summary>
/// Service for accessing widget type definitions.
/// </summary>
public static class WidgetDefinitionService
{
    private static readonly Dictionary<string, IWidgetDefinition> Definitions;

    static WidgetDefinitionService()
    {
        var definitions = new IWidgetDefinition[]
        {
            new HeroWidgetDefinition(),
            new WysiwygWidgetDefinition(),
            new ImageTextWidgetDefinition(),
            new CardGridWidgetDefinition(),
            new FaqWidgetDefinition(),
            new CtaWidgetDefinition(),
            new EmbedWidgetDefinition(),
            new ContentListWidgetDefinition(),
        };

        Definitions = definitions.ToDictionary(
            d => d.DeveloperName,
            d => d,
            StringComparer.OrdinalIgnoreCase
        );
    }

    /// <summary>
    /// Gets all available widget definitions.
    /// </summary>
    public static IEnumerable<IWidgetDefinition> GetAll() => Definitions.Values;

    /// <summary>
    /// Gets a widget definition by developer name.
    /// </summary>
    /// <param name="developerName">The widget type developer name.</param>
    /// <returns>The widget definition, or null if not found.</returns>
    public static IWidgetDefinition? GetByDeveloperName(string developerName)
    {
        if (string.IsNullOrEmpty(developerName))
            return null;

        return Definitions.TryGetValue(developerName, out var definition) ? definition : null;
    }

    /// <summary>
    /// Checks if a widget type is valid/supported.
    /// </summary>
    /// <param name="developerName">The widget type developer name.</param>
    /// <returns>True if the widget type is supported.</returns>
    public static bool IsValidWidgetType(string developerName)
    {
        return !string.IsNullOrEmpty(developerName) && Definitions.ContainsKey(developerName);
    }

    /// <summary>
    /// Gets the admin summary for a widget instance.
    /// </summary>
    /// <param name="widgetType">The widget type developer name.</param>
    /// <param name="settingsJson">The widget settings JSON.</param>
    /// <returns>A summary string for the admin UI.</returns>
    public static string GetAdminSummary(string widgetType, string settingsJson)
    {
        var definition = GetByDeveloperName(widgetType);
        if (definition == null)
            return "Unknown widget type";

        return definition.GetAdminSummary(settingsJson);
    }
}

