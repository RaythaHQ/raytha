using System.Text.Json;

namespace Raytha.Domain.Entities;

/// <summary>
/// A field stored as JSON rather than as a row: the fields of a widget template, and the
/// sub-fields of a repeater. FieldType is a field type developer name; widget templates
/// also allow the widget-only types (image, content_type, view).
/// </summary>
public record FieldDefinition
{
    public string DeveloperName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsRequired { get; init; }
    public JsonElement? DefaultValue { get; init; }
    public IReadOnlyList<ContentTypeFieldChoice> Choices { get; init; } = [];
    public IReadOnlyList<FieldDefinition> SubFields { get; init; } = [];

    /// <summary>
    /// For a view field, the developer name of the sibling content_type field whose views it lists.
    /// </summary>
    public string? ContentTypeField { get; init; }

    public static IReadOnlyList<FieldDefinition> ListFromJson(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<FieldDefinition>>(json, JsonOptions) ?? [];

    public static string ListToJson(IEnumerable<FieldDefinition> fields) =>
        JsonSerializer.Serialize(fields, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
