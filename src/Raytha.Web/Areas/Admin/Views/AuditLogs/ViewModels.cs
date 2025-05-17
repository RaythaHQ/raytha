using System;
using System.Collections.Generic;
using CSharpVitamins;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

namespace Raytha.Web.Areas.Admin.Views.AuditLogs;

public class AuditLogsPagination_ViewModel : Pagination_ViewModel
{
    public IEnumerable<AuditLogsListItem_ViewModel> Items { get; }

    public AuditLogsPagination_ViewModel(
        IEnumerable<AuditLogsListItem_ViewModel> items,
        int totalCount
    )
        : base(totalCount) => Items = items;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    //helpers
    public Dictionary<string, string> LogCategories { get; set; }

    public string OrderByThisColumn(string currentColumn)
    {
        if (OrderByPropertyName == currentColumn && OrderByDirection == SortOrder.Descending)
        {
            return $"{currentColumn} {SortOrder.Ascending}";
        }
        else
        {
            return $"{currentColumn} {SortOrder.Descending}";
        }
    }
}

public class AuditLogsListItem_ViewModel
{
    public ShortGuid Id { get; set; }
    public string CreationTime { get; set; }
    public string Category { get; set; }
    public string UserEmail { get; set; }
    public string EntityId { get; set; }
    public string Request { get; set; }
    public string IpAddress { get; set; }
}
