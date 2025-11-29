using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.SitePages.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.SitePages;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.SitePagesListItemViewModel>
{
    public ListViewModel<SitePagesListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Title {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Site Pages",
                RouteName = RouteNames.SitePages.Index,
                IsActive = true,
                Icon = SidebarIcons.SitePages,
            }
        );

        var input = new GetSitePages.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new SitePagesListItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            RoutePath = p.RoutePath,
            IsPublished = p.IsPublished.YesOrNo(),
            IsDraft = p.IsDraft.YesOrNo(),
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.LastModificationTime
            ),
            LastModifierUser = p.LastModifierUser != null ? p.LastModifierUser.FullName : "N/A",
            IsHomePage = CurrentOrganization.HomePageId == p.Id,
        });

        ListView = new ListViewModel<SitePagesListItemViewModel>(items, response.Result.TotalCount);
        return Page();
    }

    public record SitePagesListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; }

        [Display(Name = "URL Path")]
        public string RoutePath { get; init; }

        [Display(Name = "Published")]
        public string IsPublished { get; init; }

        [Display(Name = "Has Draft")]
        public string IsDraft { get; init; }

        [Display(Name = "Last modified")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Modified by")]
        public string LastModifierUser { get; init; }

        // Helper property
        public bool IsHomePage { get; init; }
    }
}

