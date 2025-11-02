#nullable enable
namespace Raytha.Web.Areas.Shared.Models;

/// <summary>
/// Standard pagination and sorting query parameters for list pages.
/// </summary>
public record PaginationQuery
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Gets or sets the orderBy clause (e.g., "Name asc").
    /// </summary>
    public string? OrderBy { get; init; }

    /// <summary>
    /// Gets or sets the search query string.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Gets or sets the filter string for additional filtering.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>
    /// Validates and clamps the page number to a minimum of 1.
    /// </summary>
    public int ValidatedPageNumber => Math.Max(PageNumber, 1);

    /// <summary>
    /// Validates and clamps the page size to an acceptable range (1-200).
    /// Per raytha.instructions.md, max page size is 200.
    /// </summary>
    public int ValidatedPageSize => Math.Clamp(PageSize == 0 ? 50 : PageSize, 1, 200);
}

