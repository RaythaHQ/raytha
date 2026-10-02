using System.Text.RegularExpressions;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Themes.WidgetTemplates;

/// <summary>
/// Rules for the fields of a widget template. A field's DeveloperName becomes a SettingsJson key and
/// a Liquid <c>widget.settings.&lt;name&gt;</c> path, so camelCase is allowed.
/// </summary>
public static partial class WidgetFieldDefinitions
{
    public static IEnumerable<string> Validate(IReadOnlyList<FieldDefinition> fields) => Validate(fields, null);

    private static IEnumerable<string> Validate(IReadOnlyList<FieldDefinition> fields, FieldDefinition? repeater)
    {
        var prefix = repeater is null ? string.Empty : $"{Name(repeater)}: ";

        foreach (
            var duplicate in fields
                .GroupBy(f => f.DeveloperName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
        )
        {
            yield return $"{prefix}Developer name '{duplicate.Key}' is used by more than one field.";
        }

        foreach (var field in fields)
        {
            foreach (var error in ValidateField(field, fields, repeater))
            {
                yield return prefix + error;
            }
        }
    }

    private static IEnumerable<string> ValidateField(
        FieldDefinition field,
        IReadOnlyList<FieldDefinition> siblings,
        FieldDefinition? repeater
    )
    {
        var name = Name(field);

        if (!DeveloperNamePattern().IsMatch(field.DeveloperName ?? string.Empty))
            yield return $"Developer name '{field.DeveloperName}' must start with a letter and contain only letters, digits, and underscores.";

        if (string.IsNullOrWhiteSpace(field.Label))
            yield return $"Field '{field.DeveloperName}' needs a label.";

        if (!WidgetFieldType.IsSupported(field.FieldType))
        {
            yield return $"{name} has an unsupported field type '{field.FieldType}'.";
            yield break;
        }

        var type = WidgetFieldType.From(field.FieldType);

        if (repeater is not null && !type.AllowedInRepeater)
            yield return $"{name} cannot be a repeater inside a repeater.";

        if (type.HasChoices)
        {
            if (field.Choices.Count == 0)
                yield return $"{name} needs at least one choice.";
            if (field.Choices.Any(c => string.IsNullOrWhiteSpace(c.DeveloperName) || string.IsNullOrWhiteSpace(c.Label)))
                yield return $"{name} has a choice without a label or developer name.";
            if (field.Choices.GroupBy(c => c.DeveloperName).Any(g => g.Count() > 1))
                yield return $"{name} has duplicate choice developer names.";
        }
        else if (field.Choices.Count > 0)
        {
            yield return $"{name} cannot have choices; only dropdown and radio fields do.";
        }

        if (type.Equals(WidgetFieldType.Repeater))
        {
            if (field.SubFields.Count == 0)
                yield return $"{name} needs at least one sub-field.";
            foreach (var error in Validate(field.SubFields, field))
                yield return error;
        }
        else if (field.SubFields.Count > 0)
        {
            yield return $"{name} cannot have sub-fields; only repeater fields do.";
        }

        if (type.Equals(WidgetFieldType.View))
        {
            var source = siblings.FirstOrDefault(f => f.DeveloperName == field.ContentTypeField);
            if (source is null || source.FieldType != WidgetFieldType.ContentType)
                yield return $"{name} must name a content type field of the same template in ContentTypeField.";
        }
        else if (!string.IsNullOrEmpty(field.ContentTypeField))
        {
            yield return $"{name} cannot set ContentTypeField; only view fields do.";
        }

        foreach (var error in WidgetFieldType.Validate(field with { IsRequired = false }, field.DefaultValue))
            yield return $"Default value: {error}";
    }

    private static string Name(FieldDefinition field) =>
        string.IsNullOrWhiteSpace(field.Label) ? $"Field '{field.DeveloperName}'" : $"Field '{field.Label}'";

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]*$")]
    private static partial Regex DeveloperNamePattern();
}
