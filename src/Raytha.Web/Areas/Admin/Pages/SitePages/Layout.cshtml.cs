using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.SitePages.Widgets;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Layout : BaseAdminPageModel
{
    public SitePageViewModel SitePage { get; set; }
    public IEnumerable<WidgetTypeViewModel> AvailableWidgetTypes { get; set; }
    public string WidgetsJson { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetSitePageById.Query { Id = id });

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Site Pages",
                RouteName = RouteNames.SitePages.Index,
                IsActive = false,
                Icon = SidebarIcons.SitePages,
            },
            new BreadcrumbNode
            {
                Label = response.Result.Title,
                RouteName = RouteNames.SitePages.Edit,
                RouteValues = new Dictionary<string, string> { ["id"] = id },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Layout",
                RouteName = RouteNames.SitePages.Layout,
                IsActive = true,
            }
        );

        // Convert widgets to view model with admin summaries
        var widgetsWithSummaries = response.Result.Widgets.ToDictionary(
            kvp => kvp.Key,
            kvp =>
                kvp.Value.Select(w => new WidgetInstanceViewModel
                    {
                        Id = w.Id,
                        WidgetType = w.WidgetType,
                        SettingsJson = w.SettingsJson,
                        Row = w.Row,
                        Column = w.Column,
                        ColumnSpan = w.ColumnSpan,
                        DisplayName =
                            WidgetDefinitionService.GetByDeveloperName(w.WidgetType)?.DisplayName
                            ?? w.WidgetType,
                        IconClass =
                            WidgetDefinitionService.GetByDeveloperName(w.WidgetType)?.IconClass
                            ?? "bi-puzzle",
                        AdminSummary = WidgetDefinitionService.GetAdminSummary(
                            w.WidgetType,
                            w.SettingsJson
                        ),
                    })
                    .ToList()
        );

        SitePage = new SitePageViewModel
        {
            Id = response.Result.Id,
            Title = response.Result.Title,
            RoutePath = response.Result.RoutePath,
            IsPublished = response.Result.IsPublished,
            IsDraft = response.Result.IsDraft,
            Widgets = widgetsWithSummaries,
        };

        // Serialize widgets for JavaScript
        WidgetsJson = JsonSerializer.Serialize(
            widgetsWithSummaries,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        // Load available widget types
        AvailableWidgetTypes = WidgetDefinitionService
            .GetAll()
            .Select(w => new WidgetTypeViewModel
            {
                DeveloperName = w.DeveloperName,
                DisplayName = w.DisplayName,
                Description = w.Description,
                IconClass = w.IconClass,
            });

        return Page();
    }

    public async Task<IActionResult> OnPostSaveLayout(
        string id,
        [FromBody] SaveLayoutRequest request
    )
    {
        var input = new SaveWidgets.Command
        {
            Id = id,
            SectionName = request.SectionName,
            Widgets = request.Widgets.Select(w => new SaveWidgets.WidgetInput
            {
                Id = w.Id,
                WidgetType = w.WidgetType,
                SettingsJson = w.SettingsJson,
                Row = w.Row,
                Column = w.Column,
                ColumnSpan = w.ColumnSpan,
            }),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            return new JsonResult(new { success = true });
        }
        else
        {
            return new JsonResult(new { success = false, errors = response.GetErrors() });
        }
    }

    public record SaveLayoutRequest
    {
        public string SectionName { get; init; } = "main";
        public List<WidgetInputModel> Widgets { get; init; } = new();
    }

    public record WidgetInputModel
    {
        public string? Id { get; init; }
        public string WidgetType { get; init; } = string.Empty;
        public string SettingsJson { get; init; } = "{}";
        public int Row { get; init; }
        public int Column { get; init; }
        public int ColumnSpan { get; init; } = 12;
    }

    public record SitePageViewModel
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string RoutePath { get; init; }
        public bool IsPublished { get; init; }
        public bool IsDraft { get; init; }
        public Dictionary<string, List<WidgetInstanceViewModel>> Widgets { get; init; }
    }

    public record WidgetInstanceViewModel
    {
        public string Id { get; init; }
        public string WidgetType { get; init; }
        public string SettingsJson { get; init; }
        public int Row { get; init; }
        public int Column { get; init; }
        public int ColumnSpan { get; init; }
        public string DisplayName { get; init; }
        public string IconClass { get; init; }
        public string AdminSummary { get; init; }
    }

    public record WidgetTypeViewModel
    {
        public string DeveloperName { get; init; }
        public string DisplayName { get; init; }
        public string Description { get; init; }
        public string IconClass { get; init; }
    }
}
