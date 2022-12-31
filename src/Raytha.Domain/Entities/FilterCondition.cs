namespace Raytha.Domain.Entities;

public record FilterCondition
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public FilterConditionType? Type { get; init; }
    public BooleanOperator? GroupOperator { get; init; }
    public string? Field { get; init; }
    public ConditionOperator? ConditionOperator { get; init; }
    public string? Value { get; init; }
}
