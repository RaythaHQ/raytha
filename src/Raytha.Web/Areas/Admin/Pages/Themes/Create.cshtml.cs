using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IActionResult OnGet()
    {
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
                Label = "Create",
                RouteName = RouteNames.Themes.Create,
                IsActive = true,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateTheme.Command
        {
            Title = Form.Title,
            DeveloperName = Form.DeveloperName,
            Description = Form.Description,
            InsertDefaultThemeMediaItems = Form.InsertDefaultThemeMediaItems,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Title} was created successfully.");
            return RedirectToPage(RouteNames.Themes.Edit, new { id = response.Result.Guid });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this theme. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Title")]
        public string Title { get; init; }

        [Display(Name = "Description")]
        public string Description { get; init; }

        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Insert default theme media items")]
        public bool InsertDefaultThemeMediaItems { get; set; } = true;
    }
}
