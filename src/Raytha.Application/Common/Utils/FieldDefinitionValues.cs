using System.Globalization;
using System.Text.RegularExpressions;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Domain.ValueObjects.FieldValues;

namespace Raytha.Application.Common.Utils;

/// <summary>
/// Checks a raw value against a <see cref="FieldDefinition"/> and returns the form to store:
/// a lowercase <c>#rrggbb</c> for colors, typed cells for repeater rows.
/// </summary>
public static class FieldDefinitionValues
{
    /// <summary>
    /// Content fields whose stored form is the canonical one from <see cref="Parse"/>. Older types
    /// keep what the client sent, because existing filters and sorts read that raw text. A
    /// relationship is stored as a full Guid because list queries cast it to <c>uuid</c> to join
    /// the related item. A date is stored as ISO so it sorts and filters the same under every
    /// organization date format.
    /// </summary>
    private static readonly HashSet<string> CanonicalContentFieldTypes =
    [
        BaseFieldType.Color.DeveloperName,
        BaseFieldType.Repeater.DeveloperName,
        BaseFieldType.OneToOneRelationship.DeveloperName,
        BaseFieldType.Date.DeveloperName,
    ];

    private static readonly Regex IsoDatePrefix = new(@"^\d{4}-\d{2}-\d{2}", RegexOptions.Compiled);

    public static FieldDefinition ToDefinition(this ContentTypeField field) =>
        new()
        {
            DeveloperName = field.DeveloperName ?? string.Empty,
            Label = field.Label ?? string.Empty,
            FieldType = field.FieldType.DeveloperName,
            Description = field.Description,
            IsRequired = field.IsRequired,
            Choices = field.Choices.ToList(),
            SubFields = field.SubFields,
        };

    /// <summary>
    /// Errors in a parsed content field value that its field type cannot see on its own: each
    /// repeater row checked against the repeater's sub-fields.
    /// </summary>
    public static IReadOnlyList<string> RowErrors(
        ContentTypeField field,
        BaseFieldValue value,
        bool enforceRequired
    )
    {
        var errors = new List<string>();
        if (value is RepeaterFieldValue rows)
        {
            var definition = field.ToDefinition();
            ParseRows(definition, LabelOf(definition), rows, enforceRequired, errors);
        }
        return errors;
    }

    /// <summary>
    /// The content to persist: color, repeater, relationship, and date fields replaced by their
    /// canonical form, everything else as sent.
    /// </summary>
    public static IDictionary<string, dynamic> ToStoredContent(
        IEnumerable<ContentTypeField> fields,
        IDictionary<string, dynamic> content
    )
    {
        var stored = new Dictionary<string, dynamic>(content);
        foreach (var field in fields)
        {
            if (
                field.DeveloperName != null
                && CanonicalContentFieldTypes.Contains(field.FieldType.DeveloperName)
                && stored.TryGetValue(field.DeveloperName, out object? raw)
            )
            {
                stored[field.DeveloperName] = Parse(field.ToDefinition(), raw, false, new List<string>())!;
            }
        }
        return stored;
    }

    public static object? Parse(
        FieldDefinition definition,
        object? raw,
        bool enforceRequired,
        ICollection<string> errors
    )
    {
        var label = LabelOf(definition);

        BaseFieldValue value;
        try
        {
            value = BaseFieldType.From(definition.FieldType).FieldValueFrom(raw);
        }
        catch (Exception)
        {
            errors.Add($"'{label}' is an invalid format.");
            return null;
        }

        if (enforceRequired && definition.IsRequired && !value.HasValue)
        {
            errors.Add($"'{label}' field is required.");
        }

        switch (value)
        {
            case RepeaterFieldValue rows:
                return ParseRows(definition, label, rows, enforceRequired, errors);
            case ColorFieldValue color:
                return color.Value;
            case DecimalFieldValue number:
                return number.Value;
            case BooleanFieldValue flag:
                return flag.HasValue && flag.Value;
            case DateTimeFieldValue date:
                return date.HasValue ? IsoDate(raw!.ToString()!.Trim(), ((DateTime?)date)!.Value) : null;
        }

        if (!value.HasValue)
        {
            return null;
        }

        if (
            definition.Choices.Count > 0
            && !definition.Choices.Any(c => c.DeveloperName == value.Text)
        )
        {
            errors.Add($"'{label}' must be one of its choices.");
        }
        return value.Text;
    }

    private static List<Dictionary<string, object?>> ParseRows(
        FieldDefinition definition,
        string label,
        RepeaterFieldValue value,
        bool enforceRequired,
        ICollection<string> errors
    )
    {
        var rows = new List<Dictionary<string, object?>>();
        var rowNumber = 0;
        foreach (var row in value.Rows())
        {
            rowNumber++;
            var rowErrors = new List<string>();
            var stored = new Dictionary<string, object?>();
            foreach (var subField in definition.SubFields)
            {
                row.TryGetValue(subField.DeveloperName, out var cell);
                stored[subField.DeveloperName] = Parse(subField, cell, enforceRequired, rowErrors);
            }
            foreach (var error in rowErrors)
            {
                errors.Add($"{label}, row {rowNumber}: {error}");
            }
            rows.Add(stored);
        }
        return rows;
    }

    private static string IsoDate(string raw, DateTime parsed) =>
        IsoDatePrefix.IsMatch(raw) ? raw
        : parsed.TimeOfDay == TimeSpan.Zero ? parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        : parsed.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static string LabelOf(FieldDefinition definition) =>
        string.IsNullOrEmpty(definition.Label) ? definition.DeveloperName : definition.Label;
}
