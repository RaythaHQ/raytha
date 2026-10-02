using System.Text.Encodings.Web;
using System.Text.Json;

namespace Raytha.Domain.ValueObjects.FieldValues;

/// <summary>
/// Rows of a repeater: a JSON array of objects keyed by sub-field developer name, each cell a
/// string, number, boolean, or null. <see cref="Value"/> is the rows, so Liquid reads
/// <c>{% for row in Target.PublishedContent.faq.Value %}{{ row.question }}{% endfor %}</c>.
/// </summary>
public record RepeaterFieldValue : BaseFieldValue
{
    private readonly List<Dictionary<string, object?>> _rows = [];

    public RepeaterFieldValue(object? value)
    {
        var element = ToElement(value);
        if (element is not { } array)
            return;

        if (array.ValueKind != JsonValueKind.Array)
            throw new FormatException("A repeater value must be a JSON array of rows.");

        foreach (var row in array.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
                throw new FormatException("Each repeater row must be a JSON object.");

            var cells = new Dictionary<string, object?>();
            foreach (var cell in row.EnumerateObject())
            {
                cells[cell.Name] = ToCell(cell.Value);
            }
            _rows.Add(cells);
        }
    }

    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows() => _rows;

    public override dynamic Value => _rows;
    public override string Text => ToString();
    public override bool HasValue => _rows.Count > 0;

    public override string ToString() =>
        _rows.Count > 0 ? JsonSerializer.Serialize(_rows, TextOptions) : string.Empty;

    private static readonly JsonSerializerOptions TextOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static JsonElement? ToElement(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return string.IsNullOrWhiteSpace(text) ? null : ParseText(text);
            case JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }:
                return null;
            case JsonElement { ValueKind: JsonValueKind.String } element:
                return ToElement(element.GetString());
            case JsonElement element:
                return element;
            default:
                return JsonSerializer.SerializeToElement(value);
        }
    }

    private static JsonElement ParseText(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new FormatException("A repeater value must be a JSON array of rows.", ex);
        }
    }

    private static object? ToCell(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new FormatException("Repeater cells must be text, numbers, booleans, or empty."),
        };
}
