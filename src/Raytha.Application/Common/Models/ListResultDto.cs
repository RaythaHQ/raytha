namespace Raytha.Application.Common.Models;

public record ListResultDto<T> where T : class
{
    public ListResultDto(IEnumerable<T> items, int totalCount)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount));

        Items = items;
        TotalCount = totalCount;
    }

    public IEnumerable<T> Items { get; init; }
    public int TotalCount { get; init; }
}