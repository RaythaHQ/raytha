namespace Raytha.Infrastructure.JsonQueryEngine.Filtering;

internal enum ComparisonOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
}

internal enum MatchKind
{
    Contains,
    StartsWith,
    EndsWith,
}

internal abstract record FilterNode;

internal sealed record AndNode(FilterNode Left, FilterNode Right) : FilterNode;

internal sealed record OrNode(FilterNode Left, FilterNode Right) : FilterNode;

internal sealed record NotNode(FilterNode Operand) : FilterNode;

internal sealed record ComparisonNode(string Field, ComparisonOperator Operator, string Value)
    : FilterNode;

internal sealed record MatchNode(string Field, MatchKind Kind, string Value) : FilterNode;

internal sealed record NullCheckNode(string Field, bool Negated) : FilterNode;
