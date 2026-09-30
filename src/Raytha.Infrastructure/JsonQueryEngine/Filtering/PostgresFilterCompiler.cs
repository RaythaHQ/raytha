using System.Globalization;
using CSharpVitamins;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Infrastructure.JsonQueryEngine.Filtering;

/// <summary>
/// Compiles a <see cref="FilterNode"/> tree into a Postgres boolean expression. Every user value is
/// registered through <paramref name="addParameter"/> and referenced by placeholder, so no value is
/// ever concatenated into the SQL text.
/// </summary>
internal sealed class PostgresFilterCompiler
{
    private readonly ContentTypeFieldResolver _resolver;

    public PostgresFilterCompiler(ContentTypeFieldResolver resolver)
    {
        _resolver = resolver;
    }

    public string Compile(FilterNode node, Func<object?, string> addParameter)
    {
        switch (node)
        {
            case AndNode and:
                return $"({Compile(and.Left, addParameter)} AND {Compile(and.Right, addParameter)})";
            case OrNode or:
                return $"({Compile(or.Left, addParameter)} OR {Compile(or.Right, addParameter)})";
            case NotNode not:
                return $"(NOT {Compile(not.Operand, addParameter)})";
            case NullCheckNode nullCheck:
                return CompileNullCheck(nullCheck);
            case ComparisonNode comparison:
                return CompileComparison(comparison, addParameter);
            case MatchNode match:
                return CompileMatch(match, addParameter);
            default:
                throw new InvalidFilterException("Unsupported filter expression.");
        }
    }

    private string CompileNullCheck(NullCheckNode node)
    {
        var field = _resolver.Resolve(node.Field);
        return $"({field.ScalarSql}{(node.Negated ? " IS NOT NULL" : " IS NULL")})";
    }

    private string CompileComparison(ComparisonNode node, Func<object?, string> addParameter)
    {
        var field = _resolver.Resolve(node.Field);
        if (field.Kind is FilterFieldKind.MultiSelect or FilterFieldKind.Repeater)
            throw new InvalidFilterException(
                $"Field '{node.Field}' cannot be used with this operator."
            );

        var parameter = addParameter(ConvertValue(field, node.Value, _resolver.DateFormat));
        return $"({field.ScalarSql} {Operator(node.Operator)} {parameter})";
    }

    private string CompileMatch(MatchNode node, Func<object?, string> addParameter)
    {
        var field = _resolver.Resolve(node.Field);
        switch (field.Kind)
        {
            case FilterFieldKind.Text:
            case FilterFieldKind.Relationship:
            case FilterFieldKind.Number:
            case FilterFieldKind.Date:
            {
                var pattern = PostgresFieldSql.WrapLike(
                    node.Kind,
                    PostgresFieldSql.EscapeLike(node.Value)
                );
                var parameter = addParameter(pattern);
                return $"({field.TextSql} ILIKE {parameter} ESCAPE '\\')";
            }
            case FilterFieldKind.MultiSelect:
                if (node.Kind != MatchKind.Contains)
                    throw new InvalidFilterException(
                        $"Field '{node.Field}' can only be filtered with contains."
                    );
                if (node.Value == "[]")
                    return PostgresFieldSql.MultiSelectEmpty(
                        field.Alias,
                        field.JsonColumn,
                        field.ArrayKey
                    );
                var itemParameter = addParameter(node.Value);
                return PostgresFieldSql.MultiSelectContains(
                    field.Alias,
                    field.JsonColumn,
                    field.ArrayKey,
                    itemParameter
                );
            case FilterFieldKind.Repeater:
                if (node.Kind != MatchKind.Contains || node.Value != "[]")
                    throw new InvalidFilterException(
                        $"Field '{node.Field}' can only be filtered by empty or not empty."
                    );
                return PostgresFieldSql.RepeaterEmpty(field.Alias, field.JsonColumn, field.ArrayKey);
            default:
                throw new InvalidFilterException(
                    $"Field '{node.Field}' does not support text matching."
                );
        }
    }

    private static object ConvertValue(ResolvedField field, string value, string dateFormat)
    {
        switch (field.ValueType)
        {
            case FilterValueType.Number:
                if (
                    !decimal.TryParse(
                        value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out var number
                    )
                )
                    throw new InvalidFilterException(
                        $"Field '{field.Name}' expects a numeric value."
                    );
                return number;
            case FilterValueType.Guid:
                return ParseGuid(value);
            case FilterValueType.Boolean:
                if (!bool.TryParse(value, out var flag))
                    throw new InvalidFilterException($"Field '{field.Name}' expects true or false.");
                return flag;
            case FilterValueType.Date:
                return DateTime.SpecifyKind(
                    ParseDate(field, value, dateFormat, DateTimeStyles.None).Date,
                    DateTimeKind.Unspecified
                );
            case FilterValueType.Timestamp:
                return DateTime.SpecifyKind(
                    ParseDate(
                        field,
                        value,
                        dateFormat,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                    ),
                    DateTimeKind.Utc
                );
            default:
                return value;
        }
    }

    /// <summary>
    /// Accepts the organization's date format (what the Razor admin wrote) or an ISO date (what the
    /// admin SPA and API clients send).
    /// </summary>
    private static DateTime ParseDate(
        ResolvedField field,
        string value,
        string dateFormat,
        DateTimeStyles styles
    )
    {
        var trimmed = value.Trim();
        if (
            !string.IsNullOrEmpty(dateFormat)
            && DateTime.TryParseExact(
                trimmed,
                dateFormat,
                CultureInfo.InvariantCulture,
                styles,
                out var inOrganizationFormat
            )
        )
            return inOrganizationFormat;
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, styles, out var parsed))
            return parsed;
        throw new InvalidFilterException($"Field '{field.Name}' expects a date.");
    }

    private static Guid ParseGuid(string value)
    {
        var candidate = value.StartsWith("guid_", StringComparison.Ordinal)
            ? value.Substring("guid_".Length)
            : value;

        if (ShortGuid.TryParse(candidate, out ShortGuid shortGuid))
            return shortGuid.Guid;
        if (Guid.TryParse(candidate, out var guid))
            return guid;
        return Guid.Empty;
    }

    private static string Operator(ComparisonOperator op) =>
        op switch
        {
            ComparisonOperator.Equal => "=",
            ComparisonOperator.NotEqual => "!=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.LessThanOrEqual => "<=",
            _ => throw new InvalidFilterException("Unsupported comparison operator."),
        };
}
