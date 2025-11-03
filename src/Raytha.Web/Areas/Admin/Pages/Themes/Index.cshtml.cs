using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Themes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Themes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_TEMPLATES_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.ThemesListItemViewModel>
{
    public ListViewModel<ThemesListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Themes",
                RouteName = RouteNames.Themes.Index,
                IsActive = true,
                Icon = SidebarIcons.Themes
            }
        );

        var input = new GetThemes.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var themesResponse = await Mediator.Send(input);

        var items = themesResponse.Result.Items.Select(t => new ThemesListItemViewModel
        {
            Id = t.Id,
            Title = t.Title,
            DeveloperName = t.DeveloperName,
            Description = t.Description,
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    t.LastModificationTime
                ),
            LastModifierUser = t.LastModifierUser?.FullName ?? "N/A",
        });

        ListView = new ListViewModel<ThemesListItemViewModel>(
            items,
            themesResponse.Result.TotalCount
        );

        return Page();
    }

    public record ThemesListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; }

        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; init; }

        [Display(Name = "Last modified at:")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by:")]
        public string LastModifierUser { get; init; }
    }
}
