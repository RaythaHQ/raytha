namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

public interface ISubActionViewModel
{
    public string Id { get; set; }
    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }
}
