using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.WidgetTemplates.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes.WidgetTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Reset : BaseAdminPageModel
{
    public string ThemeId { get; set; } = string.Empty;

    public IActionResult OnGet(string themeId)
    {
        ThemeId = themeId;

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
                Label = "Widget templates",
                RouteName = RouteNames.Themes.WidgetTemplates.Index,
                RouteValues = new Dictionary<string, string> { ["themeId"] = themeId },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Reset",
                RouteName = RouteNames.Themes.WidgetTemplates.Reset,
                IsActive = true,
            }
        );

        return Page();
    }

    public async Task<IActionResult> OnPost(string themeId)
    {
        var input = new ResetWidgetTemplatesToDefault.Command { ThemeId = themeId };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage(
                "Widget templates have been reset to their default Liquid templates."
            );
            return RedirectToPage(RouteNames.Themes.WidgetTemplates.Index, new { themeId });
        }
        else
        {
            SetErrorMessage(
                "There was an error resetting widget templates",
                response.GetErrors()
            );
            return RedirectToPage(RouteNames.Themes.WidgetTemplates.Index, new { themeId });
        }
    }
}

