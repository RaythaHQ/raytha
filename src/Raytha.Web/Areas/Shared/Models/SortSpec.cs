#nullable enable
namespace Raytha.Web.Areas.Shared.Models;

/// <summary>
/// Sort specification for queries with property name and direction.
/// </summary>
public record SortSpec
{
    /// <summary>
    /// Gets the property name to sort by.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the sort direction (asc or desc).
    /// </summary>
    public required string Direction { get; init; }

    /// <summary>
    /// Parses an orderBy string (e.g., "Name asc") into a SortSpec.
    /// </summary>
    /// <param name="orderBy">The orderBy string to parse.</param>
    /// <returns>A SortSpec instance, or null if parsing fails.</returns>
    public static SortSpec? FromString(string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
            return null;

        var parts = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return null;

        var direction = parts[1].ToLowerInvariant();
        if (direction != "asc" && direction != "desc")
            return null;

        return new SortSpec { PropertyName = parts[0], Direction = direction };
    }

    /// <summary>
    /// Converts the sort specification back to a string (e.g., "Name asc").
    /// </summary>
    public override string ToString() => $"{PropertyName} {Direction}";
}
