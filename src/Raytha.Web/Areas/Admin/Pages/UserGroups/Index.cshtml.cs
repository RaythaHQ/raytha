using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.UserGroups.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.UserGroups;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.UserGroupsListItemViewModel>
{
    public ListViewModel<UserGroupsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Label {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                IsActive = false,
                Icon = SidebarIcons.Users
            },
            new BreadcrumbNode
            {
                Label = "User Groups",
                RouteName = RouteNames.UserGroups.Index,
                IsActive = true
            }
        );

        var input = new GetUserGroups.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new UserGroupsListItemViewModel
        {
            DeveloperName = p.DeveloperName,
            Label = p.Label,
            Id = p.Id,
        });

        ListView = new ListViewModel<UserGroupsListItemViewModel>(
            items,
            response.Result.TotalCount
        );
        return Page();
    }

    public record UserGroupsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "DeveloperName")]
        public string DeveloperName { get; init; }
    }
}
