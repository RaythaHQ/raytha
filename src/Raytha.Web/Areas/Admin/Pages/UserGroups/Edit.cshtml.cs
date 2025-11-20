using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.UserGroups;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
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
                Label = "Edit user group",
                RouteName = RouteNames.UserGroups.Edit,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetUserGroupById.Query { Id = id });

        Form = new FormModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
        };
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditUserGroup.Command { Id = id, Label = Form.Label };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was updated successfully.");
            return RedirectToPage(RouteNames.UserGroups.Edit, new { id });
        }
        {
            SetErrorMessage(
                "There was an error attempting to update this user group. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }
    }
}
