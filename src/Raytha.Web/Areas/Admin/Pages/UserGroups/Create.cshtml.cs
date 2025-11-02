using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.UserGroups;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "User Groups",
                RouteName = RouteNames.UserGroups.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create",
                RouteName = RouteNames.UserGroups.Create,
                IsActive = true,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateUserGroup.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(RouteNames.UserGroups.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this user group. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }
    }
}
