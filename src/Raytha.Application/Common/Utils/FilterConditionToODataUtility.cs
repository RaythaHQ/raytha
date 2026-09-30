using System.Text;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;

namespace Raytha.Application.Common.Utils;

public class FilterConditionToODataUtility
{
    private ContentType _contentType;

    public FilterConditionToODataUtility(ContentType contentType)
    {
        _contentType = contentType;
    }

    public string ToODataFilter(IEnumerable<FilterCondition> filter)
    {
        if (filter == null || !filter.Any())
            return string.Empty;

        FilterCondition baseFilterGroup = filter.First(p => p.ParentId == null);

        return BuildODataFilter(filter.Where(p => p.ParentId != null), baseFilterGroup);
    }

    private string BuildODataFilter(IEnumerable<FilterCondition> children, FilterCondition current)
    {
        if (children != null && children.Any())
        {
            List<string> oDataSubtrees = new List<string>();
            foreach (var child in children)
            {
                oDataSubtrees.Add(
                    BuildODataFilter(children.ToList().Where(p => p.ParentId == child.Id), child)
                );
            }
            string joinedSubtrees = string.Join(
                $" {current.GroupOperator} ",
                oDataSubtrees.Where(p => !string.IsNullOrEmpty(p))
            );
            if (!string.IsNullOrEmpty(joinedSubtrees))
                return $"({joinedSubtrees})";
            else
                return string.Empty;
        }

        if (current != null && current.Type.DeveloperName == FilterConditionType.FilterCondition)
        {
            string translatedExpression = TranslateToODataExpression(current);
            if (!string.IsNullOrEmpty(translatedExpression))
                return $"{translatedExpression}";
            else
                return string.Empty;
        }
        return string.Empty;
    }

    private string TranslateToODataExpression(FilterCondition condition)
    {
        StringBuilder expression = new StringBuilder();
        var value = EscapeODataStringValue(condition.Value);
        var chosenColumnAsCustomField = _contentType.ContentTypeFields.FirstOrDefault(p =>
            p.DeveloperName == condition.Field
        );

        if (condition.ConditionOperator.DeveloperName == ConditionOperator.IS_EMPTY)
        {
            if (chosenColumnAsCustomField != null && chosenColumnAsCustomField.FieldType.StoresJsonArray)
            {
                expression.Append($"{ConditionOperator.CONTAINS}({condition.Field}, '[]')");
            }
            else if (
                chosenColumnAsCustomField != null
                && chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Number
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.EQUALS} null");
            }
            else if (
                chosenColumnAsCustomField != null
                && chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Date
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.EQUALS} null");
            }
            else if (
                condition.Field == BuiltInContentTypeField.CreationTime
                || condition.Field == BuiltInContentTypeField.LastModificationTime
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.EQUALS} null");
            }
            else
            {
                expression.Append(
                    $"{condition.Field} {ConditionOperator.EQUALS} '' {BooleanOperator.OR} {condition.Field} {ConditionOperator.EQUALS} null"
                );
            }
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.IS_NOT_EMPTY)
        {
            if (chosenColumnAsCustomField != null && chosenColumnAsCustomField.FieldType.StoresJsonArray)
            {
                expression.Append($"not {ConditionOperator.CONTAINS}({condition.Field}, '[]')");
            }
            else if (
                chosenColumnAsCustomField != null
                && chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Number
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.NOT_EQUALS} null");
            }
            else if (
                chosenColumnAsCustomField != null
                && chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Date
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.NOT_EQUALS} null");
            }
            else if (
                condition.Field == BuiltInContentTypeField.CreationTime
                || condition.Field == BuiltInContentTypeField.LastModificationTime
            )
            {
                expression.Append($"{condition.Field} {ConditionOperator.NOT_EQUALS} null");
            }
            else
            {
                expression.Append(
                    $"({condition.Field} {ConditionOperator.NOT_EQUALS} '' {BooleanOperator.AND} {condition.Field} {ConditionOperator.NOT_EQUALS} null)"
                );
            }
        }
        else if (
            condition.ConditionOperator.DeveloperName == ConditionOperator.CONTAINS
            || condition.ConditionOperator.DeveloperName == ConditionOperator.HAS
        )
        {
            expression.Append(
                $"{ConditionOperator.CONTAINS}({condition.Field}, '{value}')"
            );
        }
        else if (
            condition.ConditionOperator.DeveloperName == ConditionOperator.NOT_CONTAINS
            || condition.ConditionOperator.DeveloperName == ConditionOperator.NOT_HAS
        )
        {
            expression.Append(
                $"not {ConditionOperator.CONTAINS}({condition.Field}, '{value}')"
            );
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.STARTS_WITH)
        {
            expression.Append(
                $"{ConditionOperator.STARTS_WITH}({condition.Field}, '{value}')"
            );
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.NOT_STARTS_WITH)
        {
            expression.Append(
                $"not {ConditionOperator.STARTS_WITH}({condition.Field}, '{value}')"
            );
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.ENDS_WITH)
        {
            expression.Append(
                $"{ConditionOperator.ENDS_WITH}({condition.Field}, '{value}')"
            );
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.NOT_ENDS_WITH)
        {
            expression.Append(
                $"not {ConditionOperator.ENDS_WITH}({condition.Field}, '{value}')"
            );
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.IS_TRUE)
        {
            expression.Append($"{condition.Field} {ConditionOperator.EQUALS} 'true'");
        }
        else if (condition.ConditionOperator.DeveloperName == ConditionOperator.IS_FALSE)
        {
            expression.Append($"{condition.Field} {ConditionOperator.NOT_EQUALS} 'true'");
        }
        else
        {
            if (condition.Field == BuiltInContentTypeField.Id)
            {
                expression.Append(
                    $"{condition.Field} {condition.ConditionOperator} 'guid_{value}'"
                );
            }
            else
            {
                if (
                    chosenColumnAsCustomField != null
                    && chosenColumnAsCustomField.FieldType.DeveloperName == BaseFieldType.Number
                )
                {
                    expression.Append(
                        $"{condition.Field} {condition.ConditionOperator} {value}"
                    );
                }
                else
                {
                    expression.Append(
                        $"{condition.Field} {condition.ConditionOperator} '{value}'"
                    );
                }
            }
        }

        return expression.ToString();
    }

    /// <summary>
    /// Doubles single quotes so a stored filter value is a well-formed OData string literal. The
    /// engine re-parses this OData and binds the value as a SQL parameter, so a value containing a
    /// quote (an apostrophe in a name, or a hostile payload) stays inert.
    /// </summary>
    private static string EscapeODataStringValue(string? value) =>
        (value ?? string.Empty).Replace("'", "''");
}
