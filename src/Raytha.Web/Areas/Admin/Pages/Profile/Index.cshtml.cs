using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Login.Commands;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Profile;

public class Index : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Profile",
                RouteName = RouteNames.Profile.Index,
                IsActive = true,
                Icon = SidebarIcons.Users,
            }
        );

        Form = new FormModel()
        {
            FirstName = CurrentUser.FirstName,
            LastName = CurrentUser.LastName,
            EmailAddress = CurrentUser.EmailAddress,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var response = await Mediator.Send(
            new ChangeProfile.Command
            {
                Id = CurrentUser.UserId.Value,
                FirstName = Form.FirstName,
                LastName = Form.LastName,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Profile changed successfully.");
            return RedirectToPage(RouteNames.Profile.Index);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }
    }
}
