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
public class Import : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IActionResult OnGet()
    {
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
                Label = "Import",
                RouteName = RouteNames.Themes.Import,
                IsActive = true,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new BeginImportThemeFromUrl.Command
        {
            Title = Form.Title,
            DeveloperName = Form.DeveloperName,
            Description = Form.Description,
            Url = Form.Url,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Import in progress.");
            return RedirectToPage(
                RouteNames.Themes.BackgroundTaskStatus,
                new { id = response.Result }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting while importing. See the error below.",
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

        public string Url { get; set; }
    }
}
