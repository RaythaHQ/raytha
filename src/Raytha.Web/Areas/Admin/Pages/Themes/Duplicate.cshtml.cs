using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Themes;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Duplicate : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode { Label = "Themes", RouteName = RouteNames.Themes.Index, IsActive = false },
            new BreadcrumbNode { Label = "Duplicate", RouteName = RouteNames.Themes.Duplicate, IsActive = true }
        );

        var themesResponse = await Mediator.Send(
            new GetThemes.Query { OrderBy = $"CreationTime {SortOrder.ASCENDING}" }
        );

        Form = new FormModel { Themes = themesResponse.Result.Items };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new BeginDuplicateTheme.Command
        {
            Title = Form.Title,
            DeveloperName = Form.DeveloperName,
            Description = Form.Description,
            ThemeId = Form.ThemeId,
            PathBase = RelativeUrlBuilder.GetBaseUrl(),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Creating a duplicate theme in progress.");
            return RedirectToPage(RouteNames.Themes.BackgroundTaskStatus, new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to duplicate this theme. See the error below.",
                response.GetErrors()
            );

            var themesResponse = await Mediator.Send(
                new GetThemes.Query { OrderBy = $"CreationTime {SortOrder.ASCENDING}" }
            );

            Form.Themes = themesResponse.Result.Items;
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Theme")]
        public string ThemeId { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; }

        [Display(Name = "Description")]
        public string Description { get; init; }

        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; }

        public IEnumerable<ThemeDto> Themes { get; set; } = new List<ThemeDto>();
    }
}
