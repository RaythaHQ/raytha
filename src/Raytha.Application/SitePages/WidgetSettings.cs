using System.Text.Json;
using Raytha.Application.Common.Interfaces;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.SitePages;

/// <summary>
/// Checks a widget's SettingsJson against its template's fields. Keys that are not fields are
/// never inspected or removed, so settings survive a theme whose template lacks them.
/// </summary>
public static class WidgetSettings
{
    /// <summary>The fields of each widget template in the active theme, by widget type.</summary>
    public static Dictionary<string, IReadOnlyList<FieldDefinition>> ActiveThemeFields(IRaythaDbContext db)
    {
        var activeThemeId = db.OrganizationSettings.Select(o => o.ActiveThemeId).FirstOrDefault();
        return db
            .WidgetTemplates.Where(t => t.ThemeId == activeThemeId && t.DeveloperName != null)
            .Select(t => new { t.DeveloperName, t._FieldsJson })
            .AsEnumerable()
            .GroupBy(t => t.DeveloperName!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => FieldDefinition.ListFromJson(g.First()._FieldsJson), StringComparer.Ordinal);
    }

    /// <summary>
    /// <paramref name="savedSettingsJson"/> holds the settings saved for the same widget (draft and
    /// published), empty for a new one. A value equal to a saved one is accepted unchecked: data saved
    /// by an older admin or under another theme's fields must never make the widget uneditable.
    /// </summary>
    public static IEnumerable<string> Validate(
        string? settingsJson,
        IReadOnlyList<FieldDefinition> fields,
        IReadOnlyCollection<string> savedSettingsJson
    )
    {
        if (savedSettingsJson.Contains(settingsJson))
            return [];

        JsonElement settings;
        try
        {
            settings = Parse(settingsJson);
        }
        catch (JsonException)
        {
            return ["Invalid JSON format."];
        }

        if (settings.ValueKind != JsonValueKind.Object)
            return ["Settings must be a JSON object."];

        var saved = savedSettingsJson.Select(ParseObject).ToList();
        return fields
            .Where(field =>
                !saved.Any(s =>
                    JsonEquals(
                        WidgetFieldType.Property(settings, field.DeveloperName),
                        WidgetFieldType.Property(s, field.DeveloperName)
                    )
                )
            )
            .SelectMany(field => WidgetFieldType.Validate(field, WidgetFieldType.Property(settings, field.DeveloperName)))
            .ToList();
    }

    private static JsonElement Parse(string? json) =>
        JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(json) ? "{}" : json);

    private static JsonElement ParseObject(string json)
    {
        try
        {
            var element = Parse(json);
            return element.ValueKind == JsonValueKind.Object ? element : Parse("{}");
        }
        catch (JsonException)
        {
            return Parse("{}");
        }
    }

    private static bool JsonEquals(JsonElement? left, JsonElement? right) =>
        WidgetFieldType.IsEmpty(left) || WidgetFieldType.IsEmpty(right)
            ? WidgetFieldType.IsEmpty(left) && WidgetFieldType.IsEmpty(right)
            : JsonElement.DeepEquals(left!.Value, right!.Value);
}
