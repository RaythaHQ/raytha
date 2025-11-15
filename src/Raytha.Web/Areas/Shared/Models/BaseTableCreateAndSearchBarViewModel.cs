namespace Raytha.Web.Areas.Shared.Models;

public record BaseTableCreateAndSearchBarViewModel
{
    public IPaginationViewModel Pagination { get; init; }

    public string EntityName { get; set; }

    public string CreateActionName { get; init; }
}
