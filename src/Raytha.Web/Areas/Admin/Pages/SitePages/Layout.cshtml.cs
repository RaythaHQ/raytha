using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        SitePage = new SitePageViewModel
        {
            Id = response.Result.Id,
            Title = response.Result.Title,
            RoutePath = response.Result.RoutePath,
            IsPublished = response.Result.IsPublished,
            IsDraft = response.Result.IsDraft,
            Widgets = response.Result.Widgets,
        };

        // Load available widget types
        AvailableWidgetTypes = WidgetDefinitionService.GetAll().Select(w => new WidgetTypeViewModel
        {
            DeveloperName = w.DeveloperName,
            DisplayName = w.DisplayName,
            Description = w.Description,
            IconClass = w.IconClass,
        });

        return Page();
    }

    public record SitePageViewModel
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string RoutePath { get; init; }
        public bool IsPublished { get; init; }
        public bool IsDraft { get; init; }
        public Dictionary<string, List<Application.SitePages.SitePageWidgetDto>> Widgets { get; init; }
    }

    public record WidgetTypeViewModel
    {
        public string DeveloperName { get; init; }
        public string DisplayName { get; init; }
        public string Description { get; init; }
        public string IconClass { get; init; }
    }
}

