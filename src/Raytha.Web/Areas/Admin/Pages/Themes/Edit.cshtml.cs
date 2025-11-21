using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var input = new GetThemeById.Query { Id = id };

        var response = await Mediator.Send(input);

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Themes",
                RouteName = RouteNames.Themes.Index,
                IsActive = false,
                Icon = SidebarIcons.Themes,
            },
            new BreadcrumbNode
            {
                Label = "Edit theme",
                RouteName = RouteNames.Themes.Edit,
                IsActive = true,
            }
        );

        Form = new FormModel
        {
            Id = id,
            Title = response.Result.Title,
            DeveloperName = response.Result.DeveloperName,
            Description = response.Result.Description,
        };

        Id = id;
        IsActive = CurrentOrganization.ActiveThemeId == id;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditTheme.Command
        {
            Id = id,
            Title = Form.Title,
            Description = Form.Description,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Title} was updated successfully.");
            return RedirectToPage(RouteNames.Themes.Edit, new { id = response.Result.Guid });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this theme. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; }

        [Display(Name = "Developer Name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Description")]
        public string Description { get; init; }
    }
}
