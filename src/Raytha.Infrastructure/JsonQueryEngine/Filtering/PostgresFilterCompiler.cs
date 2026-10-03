using System.Globalization;
using CSharpVitamins;
using Raytha.Application.Common.Exceptions;
using Raytha.Infrastructure.JsonQueryEngine;

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
        if (field.Kind == FilterFieldKind.Repeater)
            throw new InvalidFilterException(
                $"Field '{node.Field}' cannot be used with this operator."
            );

        if (field.Kind == FilterFieldKind.MultiSelect)
            return CompileMultiSelectComparison(node, field, addParameter);

        if (
            field.Kind == FilterFieldKind.Relationship
            && TryParseRelationshipId(node.Value, out var relatedId)
        )
        {
            var idParameter = addParameter(relatedId);
            var idSql = PostgresFieldSql.ReservedColumn(field.Alias, RawSqlColumn.Id.Name);
            return Predicate(idSql, node.Operator, idParameter);
        }

        var parameter = addParameter(ConvertValue(field, node.Value));
        return Predicate(field.ScalarSql, node.Operator, parameter);
    }

    /// <summary>
    /// A comparison that is never NULL. A missing value makes a bare SQL comparison NULL, and
    /// <c>NOT NULL</c> is NULL too, so <c>ne</c> and <c>not contains()</c> would silently drop every
    /// item that has no value. Folding NULL to false first makes NOT a real negation: "not equal to
    /// Ana" includes the items nobody is assigned to, as it reads.
    /// </summary>
    private static string Predicate(string left, ComparisonOperator op, string parameter) =>
        op == ComparisonOperator.NotEqual
            ? $"(NOT COALESCE(({left} = {parameter}), FALSE))"
            : $"COALESCE(({left} {Operator(op)} {parameter}), FALSE)";

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
                return $"COALESCE(({field.TextSql} ILIKE {parameter} ESCAPE '\\'), FALSE)";
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

    /// <summary>
    /// <c>eq</c>/<c>ne</c> on a multi-select means the array contains that choice. Text
    /// <c>contains()</c> stays on the match path.
    /// </summary>
    private static string CompileMultiSelectComparison(
        ComparisonNode node,
        ResolvedField field,
        Func<object?, string> addParameter
    )
    {
        if (node.Operator is not ComparisonOperator.Equal and not ComparisonOperator.NotEqual)
            throw new InvalidFilterException(
                $"Field '{node.Field}' cannot be used with this operator. Use eq, ne, or contains."
            );

        var parameter = addParameter(node.Value);
        var match = PostgresFieldSql.MultiSelectContains(
            field.Alias,
            field.JsonColumn,
            field.ArrayKey,
            parameter
        );
        return node.Operator == ComparisonOperator.NotEqual ? $"(NOT {match})" : match;
    }

    /// <summary>
    /// A relationship filter value that is an id matches the related item. Anything else is
    /// compared to that item's primary field text.
    /// </summary>
    private static bool TryParseRelationshipId(string value, out Guid guid)
    {
        var candidate = value.StartsWith("guid_", StringComparison.Ordinal)
            ? value["guid_".Length..]
            : value;
        if (ShortGuid.TryParse(candidate, out ShortGuid shortGuid) && shortGuid != ShortGuid.Empty)
        {
            guid = shortGuid.Guid;
            return true;
        }
        if (Guid.TryParse(candidate, out guid) && guid != Guid.Empty)
            return true;
        guid = Guid.Empty;
        return false;
    }

    private static object ConvertValue(ResolvedField field, string value)
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
                    ParseDate(field, value, DateTimeStyles.None).Date,
                    DateTimeKind.Unspecified
                );
            case FilterValueType.Timestamp:
                return DateTime.SpecifyKind(
                    ParseDate(
                        field,
                        value,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                    ),
                    DateTimeKind.Utc
                );
            default:
                return value;
        }
    }

    /// <summary>An ISO date or any invariant-culture date, which is what the admin SPA and API clients send.</summary>
    private static DateTime ParseDate(ResolvedField field, string value, DateTimeStyles styles)
    {
        if (DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, styles, out var parsed))
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
