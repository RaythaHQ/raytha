namespace Raytha.Application.SitePages.Widgets;

/// <summary>
/// Defines the contract for a widget type definition.
/// Each widget type (hero, wysiwyg, etc.) has one definition.
/// </summary>
public interface IWidgetDefinition
{
    /// <summary>
    /// The developer name / system identifier (e.g., "hero", "wysiwyg").
    /// Must match BuiltInWidgetType.DeveloperName.
    /// </summary>
    string DeveloperName { get; }

    /// <summary>
    /// Human-readable display name (e.g., "Hero Banner", "WYSIWYG Editor").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Brief description of what this widget does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Icon class for display in UI (Bootstrap Icons).
    /// </summary>
    string IconClass { get; }

    /// <summary>
    /// The CLR type for this widget's settings model.
    /// </summary>
    Type SettingsType { get; }

    /// <summary>
    /// Gets a brief admin summary for display in the layout editor card.
    /// </summary>
    /// <param name="settingsJson">The widget's JSON settings.</param>
    /// <returns>A short summary string for the admin UI.</returns>
    string GetAdminSummary(string settingsJson);

    /// <summary>
    /// Creates default settings for a new widget instance.
    /// </summary>
    /// <returns>A new settings object with default values.</returns>
    object CreateDefaultSettings();
}

