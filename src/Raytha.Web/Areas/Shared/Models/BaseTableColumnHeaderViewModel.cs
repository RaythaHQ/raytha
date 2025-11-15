namespace Raytha.Web.Areas.Shared.Models;

public record BaseTableColumnHeaderViewModel
{
    public IPaginationViewModel Pagination { get; init; }
    public bool IsFirst { get; init; } = false;
    public bool IsLast { get; init; } = false;
    public string PropertyName { get; init; }
    public string DisplayName { get; init; }

    public string OrderBy
    {
        get
        {
            return
                Pagination.OrderByPropertyName == PropertyName
                && Pagination.OrderByDirection == "desc"
                ? $"{PropertyName} asc"
                : $"{PropertyName} desc";
        }
    }

    public bool IsCurrentlySorting
    {
        get { return Pagination.OrderByPropertyName == PropertyName; }
    }
}
