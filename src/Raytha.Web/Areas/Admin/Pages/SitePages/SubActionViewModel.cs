namespace Raytha.Web.Areas.Admin.Pages.SitePages;

public interface ISubActionViewModel
{
    public string Id { get; set; }
    public string? RoutePath { get; set; }
    public string Title { get; set; }
}

