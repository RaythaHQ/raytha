using Raytha.Domain.ValueObjects;

namespace Raytha.Application.Common.Models;

public record GetPagedEntitiesInputDto
{
    private const int PROPERTY_NAME_INDEX = 0;
    private const int ORDER_BY_DIRECTION_INDEX = 1;
    public virtual int PageSize { get; init; } = 50;
    public virtual int PageNumber { get; init; } = 1;
    public virtual string OrderBy { get; init; } = string.Empty;
    public virtual string Search { get; init; } = string.Empty;

    public virtual IEnumerable<GetPagedEntitiesInputDtoOrderByItem> GetOrderByItems()
    {
        List<GetPagedEntitiesInputDtoOrderByItem> orderByItems = new List<GetPagedEntitiesInputDtoOrderByItem>();
        if (string.IsNullOrEmpty(OrderBy))
            return orderByItems;

        string[] orderByItemsSplitAsArray = OrderBy.Split(",").Select(p => p.Trim()).ToArray();
        foreach (string orderByItemAsString in orderByItemsSplitAsArray)
        {
            string[] propertyAndDirection = orderByItemAsString.Split(" ").Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            if (propertyAndDirection != null && propertyAndDirection.Count() == 2 && IsValidOrderByDirection(propertyAndDirection[ORDER_BY_DIRECTION_INDEX]))
                orderByItems.Add(new GetPagedEntitiesInputDtoOrderByItem
                {
                    OrderByPropertyName = propertyAndDirection[PROPERTY_NAME_INDEX],
                    OrderByDirection = propertyAndDirection[ORDER_BY_DIRECTION_INDEX].ToLower()
                });
        }
        return orderByItems;
    }

    private bool IsValidOrderByDirection(string orderByDirection)
    {
        return orderByDirection.ToLower() == SortOrder.ASCENDING || orderByDirection.ToLower() == SortOrder.DESCENDING;
    }
}

public record GetPagedEntitiesInputDtoOrderByItem
{
    public string? OrderByPropertyName { get; init; }
    public string? OrderByDirection { get; init; }

    public override string ToString()
    {
        return $"{OrderByPropertyName} {OrderByDirection}";
    }
}