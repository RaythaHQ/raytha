using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.NavigationMenus.Commands;
using Raytha.Application.NavigationMenus.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.NavigationMenus;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    public string NavigationMenuId { get; set; }
    public bool IsNavigationMenuItem { get; set; }
    public string NavigationMenuItemId { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Menus",
                RouteName = RouteNames.NavigationMenus.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Edit",
                RouteName = RouteNames.NavigationMenus.Edit,
                IsActive = true,
            }
        );

        NavigationMenuId = id;
        IsNavigationMenuItem = false;
        NavigationMenuItemId = string.Empty;

        var input = new GetNavigationMenuById.Query { Id = id };

        var response = await Mediator.Send(input);

        Form = new FormModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            IsMainMenu = response.Result.IsMainMenu,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditNavigationMenu.Command { Id = id, Label = Form.Label };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was edited successfully.");
            return RedirectToPage(RouteNames.NavigationMenus.Edit, new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this menu. See the error below.",
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

        [Display(Name = "Is main menu")]
        public bool IsMainMenu { get; set; }
    }
}
