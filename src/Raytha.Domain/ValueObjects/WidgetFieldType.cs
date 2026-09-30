using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Raytha.Domain.Entities;
using Raytha.Domain.Exceptions;

namespace Raytha.Domain.ValueObjects;

/// <summary>
/// The field types a widget template field may use, and how a widget setting of each type is
/// checked. A setting's JSON type is never coerced: a number stays a JSON number, a checkbox a
/// JSON boolean.
/// </summary>
public partial class WidgetFieldType : ValueObject
{
    static WidgetFieldType() { }

    private WidgetFieldType() { }

    private WidgetFieldType(
        string label,
        string developerName,
        bool hasChoices,
        Func<JsonElement, FieldDefinition, string?> checkValue
    )
    {
        Label = label;
        DeveloperName = developerName;
        HasChoices = hasChoices;
        _checkValue = checkValue;
    }

    private readonly Func<JsonElement, FieldDefinition, string?> _checkValue = (_, _) => null;

    public static WidgetFieldType From(string developerName)
    {
        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == developerName);

        if (type == null)
        {
            throw new UnsupportedFieldTypeException(developerName);
        }

        return type;
    }

    public static bool IsSupported(string? developerName) =>
        SupportedTypes.Any(p => p.DeveloperName == developerName);

    public static WidgetFieldType SingleLineText => new("Single line text", "single_line_text", false, Text);
    public static WidgetFieldType LongText => new("Long text", "long_text", false, Text);
    public static WidgetFieldType Wysiwyg => new("Wysiwyg", "wysiwyg", false, Text);
    public static WidgetFieldType Number =>
        new("Number", "number", false, (v, f) => v.ValueKind == JsonValueKind.Number ? null : $"{f.Label} must be a number.");
    public static WidgetFieldType Checkbox =>
        new(
            "Checkbox",
            "checkbox",
            false,
            (v, f) => v.ValueKind is JsonValueKind.True or JsonValueKind.False ? null : $"{f.Label} must be true or false."
        );
    public static WidgetFieldType Date => new("Date", "date", false, CheckDate);
    public static WidgetFieldType Dropdown => new("Dropdown", "dropdown", true, CheckChoice);
    public static WidgetFieldType Radio => new("Radio", "radio", true, CheckChoice);
    public static WidgetFieldType Color => new("Color", "color", false, CheckColor);
    public static WidgetFieldType Repeater => new("Repeater", "repeater", false, CheckRows);

    /// <summary>A URL string, usually of an uploaded image.</summary>
    public static WidgetFieldType Image => new("Image", "image", false, Text);

    /// <summary>A content type developer name.</summary>
    public static WidgetFieldType ContentType => new("Content type", "content_type", false, Text);

    /// <summary>A view id of the content type chosen in the field named by ContentTypeField.</summary>
    public static WidgetFieldType View => new("View", "view", false, Text);

    public string Label { get; private set; } = string.Empty;
    public string DeveloperName { get; private set; } = string.Empty;
    public bool HasChoices { get; private set; }

    /// <summary>Repeater sub-fields must be scalar, so a repeater cannot nest another repeater.</summary>
    public bool AllowedInRepeater => DeveloperName != "repeater";

    /// <summary>
    /// Errors for one setting. Missing, null, and "" are empty: they fail only a required field.
    /// A field whose type is not supported is not checked.
    /// </summary>
    public static IEnumerable<string> Validate(FieldDefinition field, JsonElement? value)
    {
        if (IsEmpty(value))
        {
            if (field.IsRequired)
                yield return $"{field.Label} is required.";
            yield break;
        }

        var type = SupportedTypes.FirstOrDefault(p => p.DeveloperName == field.FieldType);
        var error = type?._checkValue(value!.Value, field);
        if (error != null)
            yield return error;
    }

    public static bool IsEmpty(JsonElement? value) =>
        value is null
        || value.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
        || (value.Value.ValueKind == JsonValueKind.String && value.Value.GetString() == string.Empty)
        || (value.Value.ValueKind == JsonValueKind.Array && value.Value.GetArrayLength() == 0);

    public static JsonElement? Property(JsonElement settings, string name) =>
        settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty(name, out var value)
            ? value
            : null;

    private static string? Text(JsonElement value, FieldDefinition field) =>
        value.ValueKind == JsonValueKind.String ? null : $"{field.Label} must be text.";

    private static string? CheckDate(JsonElement value, FieldDefinition field) =>
        value.ValueKind == JsonValueKind.String
        && DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _)
            ? null
            : $"{field.Label} must be a date.";

    private static string? CheckChoice(JsonElement value, FieldDefinition field)
    {
        var allowed = field.Choices.Where(c => !c.Disabled).Select(c => c.DeveloperName).ToList();
        return value.ValueKind == JsonValueKind.String && allowed.Contains(value.GetString())
            ? null
            : $"{field.Label} must be one of: {string.Join(", ", allowed)}.";
    }

    private static string? CheckColor(JsonElement value, FieldDefinition field) =>
        value.ValueKind == JsonValueKind.String && HexColor().IsMatch(value.GetString()!)
            ? null
            : $"{field.Label} must be a color like #1e293b.";

    private static string? CheckRows(JsonElement value, FieldDefinition field)
    {
        if (value.ValueKind != JsonValueKind.Array)
            return $"{field.Label} must be a list of rows.";

        var errors = new List<string>();
        var rowNumber = 0;
        foreach (var row in value.EnumerateArray())
        {
            rowNumber++;
            if (row.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{field.Label} row {rowNumber} must be an object.");
                continue;
            }
            errors.AddRange(
                field.SubFields.SelectMany(sub => Validate(sub, Property(row, sub.DeveloperName)))
                    .Select(error => $"{field.Label} row {rowNumber}: {error}")
            );
        }
        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();

    public static implicit operator string(WidgetFieldType type)
    {
        return type.DeveloperName;
    }

    public static explicit operator WidgetFieldType(string type)
    {
        return From(type);
    }

    public override string ToString()
    {
        return DeveloperName;
    }

    public static IEnumerable<WidgetFieldType> SupportedTypes
    {
        get
        {
            yield return SingleLineText;
            yield return LongText;
            yield return Wysiwyg;
            yield return Number;
            yield return Checkbox;
            yield return Date;
            yield return Dropdown;
            yield return Radio;
            yield return Color;
            yield return Repeater;
            yield return Image;
            yield return ContentType;
            yield return View;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeveloperName;
    }
}
