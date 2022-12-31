using CSharpVitamins;

namespace Raytha.Application.Views;

public record FilterConditionInputDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Type { get; init; } = null!;
    public string GroupOperator { get; init; } = null!;
    public string? Field { get; init; }
    public string ConditionOperator { get; init; } = null!;
    public string? Value { get; init; }
}
