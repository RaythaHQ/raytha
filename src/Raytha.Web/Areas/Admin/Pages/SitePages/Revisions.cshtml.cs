using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.SitePages.Commands;
using Raytha.Application.SitePages.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Revisions
    : BaseAdminPageModel,
        IHasListView<Revisions.SitePageRevisionsListItemViewModel>,
        ISubActionViewModel
{
    public ListViewModel<SitePageRevisionsListItemViewModel> ListView { get; set; }
    public string Id { get; set; }
    public string? RoutePath { get; set; }
    public string Title { get; set; }
    public string SitePageTitle { get; set; }

    public async Task<IActionResult> OnGet(
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var sitePageResponse = await Mediator.Send(new GetSitePageById.Query { Id = id });
        SitePageTitle = sitePageResponse.Result.Title;
        RoutePath = sitePageResponse.Result.RoutePath;
        Title = sitePageResponse.Result.Title;

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
                Label = SitePageTitle,
                RouteName = RouteNames.SitePages.Edit,
                RouteValues = new Dictionary<string, string> { ["id"] = id },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Revisions",
                RouteName = RouteNames.SitePages.Revisions,
                IsActive = true,
            }
        );

        var input = new GetSitePageRevisionsBySitePageId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new SitePageRevisionsListItemViewModel
        {
            Id = p.Id,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            CreatorUser = p.CreatorUser != null ? p.CreatorUser.FullName : "N/A",
            WidgetCount = p.PublishedWidgets.Values.Sum(list => list.Count),
            ContentAsJson = JsonSerializer.Serialize(
                p.PublishedWidgets,
                new JsonSerializerOptions { WriteIndented = true }
            ),
        });

        ListView = new ListViewModel<SitePageRevisionsListItemViewModel>(
            items,
            response.Result.TotalCount
        );

        Id = id;

        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string id, string revisionId)
    {
        var input = new RevertSitePage.Command { Id = revisionId };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage("Site page has been reverted to the selected revision.");
            return RedirectToPage(RouteNames.SitePages.Edit, new { id });
        }
        else
        {
            SetErrorMessage("There was an error reverting the site page.", response.GetErrors());
        }
        return RedirectToPage(RouteNames.SitePages.Revisions, new { id });
    }

    public record SitePageRevisionsListItemViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Created at")]
        public string CreationTime { get; set; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; set; }

        [Display(Name = "Widgets")]
        public int WidgetCount { get; set; }

        public string ContentAsJson { get; set; }
    }
}

