namespace Raytha.Web.Areas.Admin.Views.Shared.ViewModels;

public record TableCreateAndSearchBar_ViewModel
{
    public IPagination_ViewModel Model { get; init; }

    public string EntityName { get; set; }

    public string CreateActionName { get; init; }
}
