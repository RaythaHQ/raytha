using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Raytha.Application.Common.Exceptions;

namespace Raytha.Infrastructure.JsonQueryEngine.Filtering;

/// <summary>
/// Parses an OData $filter expression into a <see cref="FilterNode"/> tree. Field names and values
/// are carried as raw strings; nothing here touches SQL. Any expression the engine does not support
/// (an unknown node kind, a non-constant argument, a malformed expression) is rejected as an
/// <see cref="InvalidFilterException"/> rather than reaching a SQL string.
/// </summary>
internal static class ODataFilterParser
{
    private static readonly IEdmModel EdmModel = BuildEdmModel();
    private static readonly string EntitySetName = nameof(PlaceholderClass);

    public static FilterNode Parse(string filterExpression)
    {
        FilterClause clause;
        try
        {
            var options = new Dictionary<string, string> { { "$filter", filterExpression } };
            var navigationSource = EdmModel.FindDeclaredEntitySet(EntitySetName);
            var parser = new ODataQueryOptionParser(
                EdmModel,
                navigationSource.Type,
                navigationSource,
                options
            );
            clause = parser.ParseFilter();
        }
        catch (Exception ex)
        {
            throw new InvalidFilterException($"The filter expression could not be parsed: {ex.Message}");
        }

        return Translate(clause.Expression);
    }

    private static FilterNode Translate(QueryNode node)
    {
        switch (node)
        {
            case ConvertNode convert:
                return Translate(convert.Source);
            case UnaryOperatorNode unary when unary.OperatorKind == UnaryOperatorKind.Not:
                return new NotNode(Translate(unary.Operand));
            case BinaryOperatorNode binary:
                return TranslateBinary(binary);
            case SingleValueFunctionCallNode function:
                return TranslateFunction(function);
            default:
                throw new InvalidFilterException(
                    $"Unsupported filter expression of kind '{node.Kind}'."
                );
        }
    }

    private static FilterNode TranslateBinary(BinaryOperatorNode binary)
    {
        switch (binary.OperatorKind)
        {
            case BinaryOperatorKind.And:
                return new AndNode(Translate(binary.Left), Translate(binary.Right));
            case BinaryOperatorKind.Or:
                return new OrNode(Translate(binary.Left), Translate(binary.Right));
        }

        var field = GetFieldName(binary.Left);
        var constant = GetConstant(binary.Right);

        if (constant is null)
        {
            switch (binary.OperatorKind)
            {
                case BinaryOperatorKind.Equal:
                    return new NullCheckNode(field, Negated: false);
                case BinaryOperatorKind.NotEqual:
                    return new NullCheckNode(field, Negated: true);
                default:
                    throw new InvalidFilterException(
                        $"Operator '{binary.OperatorKind}' cannot be used with null."
                    );
            }
        }

        var op = binary.OperatorKind switch
        {
            BinaryOperatorKind.Equal => ComparisonOperator.Equal,
            BinaryOperatorKind.NotEqual => ComparisonOperator.NotEqual,
            BinaryOperatorKind.GreaterThan => ComparisonOperator.GreaterThan,
            BinaryOperatorKind.GreaterThanOrEqual => ComparisonOperator.GreaterThanOrEqual,
            BinaryOperatorKind.LessThan => ComparisonOperator.LessThan,
            BinaryOperatorKind.LessThanOrEqual => ComparisonOperator.LessThanOrEqual,
            _ => throw new InvalidFilterException(
                $"Unsupported binary operator '{binary.OperatorKind}'."
            ),
        };

        return new ComparisonNode(field, op, constant);
    }

    private static FilterNode TranslateFunction(SingleValueFunctionCallNode function)
    {
        var kind = function.Name.ToLowerInvariant() switch
        {
            "contains" => MatchKind.Contains,
            "startswith" => MatchKind.StartsWith,
            "endswith" => MatchKind.EndsWith,
            _ => throw new InvalidFilterException(
                $"Unsupported function '{function.Name}'."
            ),
        };

        var arguments = function.Parameters.ToArray();
        if (arguments.Length != 2)
            throw new InvalidFilterException(
                $"Function '{function.Name}' expects two arguments."
            );

        var field = GetFieldName(arguments[0]);
        var value =
            GetConstant(arguments[1])
            ?? throw new InvalidFilterException(
                $"Function '{function.Name}' requires a non-null value."
            );

        return new MatchNode(field, kind, value);
    }

    private static string GetFieldName(QueryNode node)
    {
        return node switch
        {
            ConvertNode convert => GetFieldName(convert.Source),
            SingleValueOpenPropertyAccessNode open => open.Name,
            SingleValuePropertyAccessNode property => property.Property.Name,
            _ => throw new InvalidFilterException(
                "A filter comparison must reference a field."
            ),
        };
    }

    private static string? GetConstant(QueryNode node)
    {
        return node switch
        {
            ConvertNode convert => GetConstant(convert.Source),
            ConstantNode constant => constant.Value is null
                ? null
                : Convert.ToString(constant.Value, CultureInfo.InvariantCulture),
            _ => throw new InvalidFilterException(
                "A filter value must be a constant."
            ),
        };
    }

    private static IEdmModel BuildEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<PlaceholderClass>(nameof(PlaceholderClass));
        return builder.GetEdmModel();
    }

    private class PlaceholderClass
    {
        [Key]
        public int Id { get; set; }
        public IDictionary<string, object> Properties { get; set; }
    }
}
