using Raytha.Application.Views;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Fields;

public interface ISubActionViewModel
{
    public string Id { get; set; }
    public ViewDto CurrentView { get; }
}
