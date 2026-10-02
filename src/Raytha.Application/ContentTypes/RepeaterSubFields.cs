using System.Text.RegularExpressions;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.ContentTypes;

/// <summary>
/// Rules for the sub-field definitions of a repeater field, shared by create and edit.
/// </summary>
public static partial class RepeaterSubFields
{
    public static IReadOnlyList<string> Errors(IReadOnlyList<FieldDefinition>? subFields)
    {
        if (subFields == null || subFields.Count == 0)
        {
            return ["A repeater needs at least one sub-field."];
        }

        var errors = new List<string>();
        for (var index = 0; index < subFields.Count; index++)
        {
            var subField = subFields[index];
            var name = string.IsNullOrWhiteSpace(subField.Label)
                ? $"Sub-field {index + 1}"
                : $"'{subField.Label}'";

            if (string.IsNullOrWhiteSpace(subField.Label))
            {
                errors.Add($"{name} needs a label.");
            }
            if (!DeveloperName().IsMatch(subField.DeveloperName.ToDeveloperName()))
            {
                errors.Add(
                    $"{name} needs a developer name that starts with a letter and uses only letters, numbers, and underscores."
                );
            }

            var fieldType = subField.FieldType?.ToLowerInvariant() ?? string.Empty;
            if (!RepeaterFieldType.SubFieldTypes.Contains(fieldType))
            {
                errors.Add($"{name} cannot use the '{subField.FieldType}' field type in a repeater.");
                continue;
            }

            if (BaseFieldType.From(fieldType).HasChoices)
            {
                errors.AddRange(ChoiceErrors(name, subField.Choices));
            }
        }

        var duplicates = subFields
            .Select(p => p.DeveloperName.ToDeveloperName())
            .Where(p => p.Length > 0)
            .GroupBy(p => p)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            errors.Add(
                $"Sub-field developer names must be unique. Duplicates: {string.Join(", ", duplicates)}"
            );
        }

        return errors;
    }

    public static IReadOnlyList<FieldDefinition> Normalize(IReadOnlyList<FieldDefinition> subFields) =>
        subFields
            .Select(subField =>
            {
                var fieldType = subField.FieldType.ToLowerInvariant();
                return new FieldDefinition
                {
                    DeveloperName = subField.DeveloperName.ToDeveloperName(),
                    Label = subField.Label.Trim(),
                    FieldType = fieldType,
                    Description = string.IsNullOrWhiteSpace(subField.Description)
                        ? null
                        : subField.Description.Trim(),
                    IsRequired = subField.IsRequired,
                    Choices = BaseFieldType.From(fieldType).HasChoices
                        ? subField
                            .Choices.Select(c => new ContentTypeFieldChoice
                            {
                                Label = c.Label?.Trim(),
                                DeveloperName = c.DeveloperName?.ToDeveloperName(),
                                Disabled = c.Disabled,
                            })
                            .ToList()
                        : [],
                };
            })
            .ToList();

    private static IEnumerable<string> ChoiceErrors(
        string name,
        IReadOnlyList<ContentTypeFieldChoice> choices
    )
    {
        if (choices.Count == 0)
        {
            yield return $"{name} needs at least one choice.";
            yield break;
        }
        if (choices.Any(c => string.IsNullOrWhiteSpace(c.Label)))
        {
            yield return $"{name} has a choice without a label.";
        }
        var developerNames = choices.Select(c => c.DeveloperName?.ToDeveloperName() ?? "").ToList();
        if (developerNames.Any(string.IsNullOrEmpty))
        {
            yield return $"{name} has a choice without a developer name.";
        }
        if (developerNames.Distinct().Count() != developerNames.Count)
        {
            yield return $"{name} has choices with the same developer name.";
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex DeveloperName();
}
