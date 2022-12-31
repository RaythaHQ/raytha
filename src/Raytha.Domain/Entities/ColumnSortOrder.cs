namespace Raytha.Domain.Entities;

public record ColumnSortOrder
{
    public string? DeveloperName { get; init; }
    public SortOrder? SortOrder { get; init; }
}
