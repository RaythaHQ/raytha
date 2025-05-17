using System;
using System.Collections.Generic;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

public interface IPagination_ViewModel
{
    string Search { get; set; }
    string Filter { get; set; }
    int PageNumber { get; set; }
    int PageSize { get; set; }
    string OrderByPropertyName { get; set; }
    string OrderByDirection { get; set; }
    string OrderByAsString { get; }
    string ActionName { get; set; }

    public int TotalCount { get; }
    public int TotalPages { get; }
    public string PreviousDisabledCss { get; }
    public string NextDisabledCss { get; }
    public int FirstVisiblePageNumber { get; }
    public int LastVisiblePageNumber { get; }
}

public abstract class Pagination_ViewModel : IPagination_ViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 0;
    public int PageSize { get; set; } = 0;
    public string OrderByPropertyName { get; set; } = string.Empty;
    public string OrderByDirection { get; set; } = string.Empty;
    public string OrderByAsString => $"{OrderByPropertyName} {OrderByDirection}";
    public string ActionName { get; set; }
    public int TotalCount { get; }

    public string PreviousDisabledCss =>
        TotalPages == 0 || PageNumber == 1 ? "disabled" : string.Empty;
    public string NextDisabledCss =>
        TotalPages == 0 || PageNumber == TotalPages ? "disabled" : string.Empty;
    public int FirstVisiblePageNumber => Math.Max(1, LastVisiblePageNumber - 3);
    public int LastVisiblePageNumber => Math.Min(TotalPages, Math.Max(1, PageNumber - 1) + 3);
    public int TotalPages => (int)Math.Ceiling((double)(TotalCount) / PageSize);

    public Pagination_ViewModel(int totalCount) => TotalCount = totalCount;

    public void SetOrderFromString(string orderBy)
    {
        var sortOrder = SplitOrderByPhrase.From(orderBy);
        if (sortOrder != null)
        {
            OrderByPropertyName = sortOrder.PropertyName;
            OrderByDirection = sortOrder.Direction;
        }
    }
}

public class List_ViewModel<T> : Pagination_ViewModel
{
    public IEnumerable<T> Items { get; }

    public List_ViewModel(IEnumerable<T> items, int totalCount)
        : base(totalCount) => Items = items;
}
