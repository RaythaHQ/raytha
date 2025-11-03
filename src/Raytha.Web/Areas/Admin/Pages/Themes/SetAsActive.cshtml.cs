using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Commands;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class SetAsActive : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGet(string id)
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
                Label = "Set as Active",
                RouteName = RouteNames.Themes.SetAsActive,
                IsActive = true,
            }
        );

        var webTemplateDeveloperNamesWithoutRelationResponse = await Mediator.Send(
            new GetWebTemplateDeveloperNamesWithoutRelation.Query { ThemeId = id }
        );

        if (webTemplateDeveloperNamesWithoutRelationResponse.Result.Any())
        {
            var newActiveThemeWebTemplateDeveloperNamesResponse = await Mediator.Send(
                new GetWebTemplateDeveloperNamesByThemeId.Query { ThemeId = id }
            );

            var model = new FormModel
            {
                ThemeId = id,
                ActiveThemeWebTemplateDeveloperNames =
                    webTemplateDeveloperNamesWithoutRelationResponse.Result,
                NewActiveThemeWebTemplateDeveloperNames =
                    newActiveThemeWebTemplateDeveloperNamesResponse.Result,
                WebTemplateMappings =
                    webTemplateDeveloperNamesWithoutRelationResponse.Result.ToDictionary(
                        dn => dn,
                        developerName =>
                            newActiveThemeWebTemplateDeveloperNamesResponse.Result.Any(dn =>
                                dn == developerName
                            )
                                ? developerName
                                : string.Empty
                    ),
            };
            Id = id;
            IsActive = CurrentOrganization.ActiveThemeId == id;
            return Page();
        }

        var input = new SetAsActiveTheme.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("The theme has been set as active successfully.");
        }
        else
        {
            SetErrorMessage(
                "An error occurred when setting the theme as active. See the error below.",
                response.GetErrors()
            );
        }
        return RedirectToPage(RouteNames.Themes.Edit, new { id });
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new BeginMatchWebTemplates.Command
        {
            Id = id,
            MatchedWebTemplateDeveloperNames = Form.WebTemplateMappings,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Set as the active theme in progress.");
            return RedirectToPage(
                RouteNames.Themes.BackgroundTaskStatus,
                new { id = response.Result }
            );
        }
        else
        {
            SetErrorMessage(
                "An error occurred when setting the theme as active. See the error below.",
                response.GetErrors()
            );
            return RedirectToPage(RouteNames.Themes.Edit, new { id });
        }
    }

    public record FormModel
    {
        public string ThemeId { get; set; }
        public IEnumerable<string> ActiveThemeWebTemplateDeveloperNames { get; set; }
        public IEnumerable<string> NewActiveThemeWebTemplateDeveloperNames { get; set; }
        public IDictionary<string, string> WebTemplateMappings { get; set; } =
            new Dictionary<string, string>();
    }
}
