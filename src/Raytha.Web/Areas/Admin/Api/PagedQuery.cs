using Microsoft.AspNetCore.Mvc;

namespace Raytha.Web.Areas.Admin.Api;

/// <summary>
/// Common list query string (<c>?pageNumber=&amp;pageSize=&amp;orderBy=&amp;search=</c>) bound with
/// <c>[AsParameters]</c>. <see cref="OrderBy"/> stays null when omitted so each Application
/// query keeps its own default ordering.
/// </summary>
public sealed class PagedQuery
{
    private const int MaxPageSize = 1000;

    [FromQuery(Name = "pageNumber")]
    public int? PageNumberRaw { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? PageSizeRaw { get; set; }

    [FromQuery(Name = "orderBy")]
    public string? OrderBy { get; set; }

    [FromQuery(Name = "search")]
    public string? SearchRaw { get; set; }

    public int PageNumber => Math.Max(PageNumberRaw ?? 1, 1);

    public int PageSize => Math.Clamp(PageSizeRaw ?? 50, 1, MaxPageSize);

    public string Search => SearchRaw ?? string.Empty;

    public bool HasOrderBy => !string.IsNullOrWhiteSpace(OrderBy);
}
