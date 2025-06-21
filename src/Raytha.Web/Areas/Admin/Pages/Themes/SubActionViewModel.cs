namespace Raytha.Web.Areas.Admin.Pages.Themes;

public interface ISubActionViewModel
{
    public string Id { get; set; }
    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }
}
