namespace Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

public record TableColumnHeader_ViewModel
{
    public IPagination_ViewModel Model { get; init; }

    public bool IsFirst { get; init; } = false;
    public bool IsLast { get; init; } = false;

    public string PropertyName { get; init; }
    public string DisplayName { get; init; }

    public string OrderBy
    {
        get
        {
            return Model.OrderByPropertyName == PropertyName && Model.OrderByDirection == "desc"
                ? $"{PropertyName} asc"
                : $"{PropertyName} desc";
        }
    }

    public bool IsCurrentlySorting
    {
        get { return Model.OrderByPropertyName == PropertyName; }
    }
}
