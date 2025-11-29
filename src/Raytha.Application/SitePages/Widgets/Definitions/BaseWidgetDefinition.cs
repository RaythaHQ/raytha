using System.Text.Json;

namespace Raytha.Application.SitePages.Widgets.Definitions;

/// <summary>
/// Base class for widget definitions providing common JSON handling.
/// </summary>
public abstract class BaseWidgetDefinition<TSettings> : IWidgetDefinition
    where TSettings : class, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public abstract string DeveloperName { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract string IconClass { get; }

    public Type SettingsType => typeof(TSettings);

    public abstract string GetAdminSummary(string settingsJson);

    public object CreateDefaultSettings() => new TSettings();

    /// <summary>
    /// Deserializes the settings JSON to the strongly-typed settings object.
    /// </summary>
    protected TSettings DeserializeSettings(string settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson) || settingsJson == "{}")
        {
            return new TSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<TSettings>(settingsJson, JsonOptions) ?? new TSettings();
        }
        catch
        {
            return new TSettings();
        }
    }

    /// <summary>
    /// Truncates a string for summary display.
    /// </summary>
    protected static string Truncate(string? value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    /// <summary>
    /// Strips HTML tags for summary display.
    /// </summary>
    protected static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", string.Empty).Trim();
    }
}

