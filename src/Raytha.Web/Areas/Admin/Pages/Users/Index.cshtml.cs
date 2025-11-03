using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

/// <summary>
/// Page model for displaying a paginated list of users.
/// </summary>
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.UsersListItemViewModel>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated user data.
    /// </summary>
    public ListViewModel<UsersListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<UsersListItemViewModel>(), 0);

    /// <summary>
    /// Handles GET requests to display the paginated list of users.
    /// </summary>
    /// <param name="search">Optional search term to filter users.</param>
    /// <param name="orderBy">Sort order specification (e.g., "LastLoggedInTime desc").</param>
    /// <param name="pageNumber">The page number to display (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The page result with user data.</returns>
    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"LastLoggedInTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default
    )
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Users",
                RouteName = RouteNames.Users.Index,
                IsActive = true,
                Icon = SidebarIcons.Users,
            }
        );

        var input = new GetUsers.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input, cancellationToken);

        var items = response.Result.Items.Select(p => new UsersListItemViewModel
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            EmailAddress = p.EmailAddress,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            LastLoggedInTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.LastLoggedInTime
            ),
            IsActive = p.IsActive.YesOrNo(),
            UserGroups = string.Join(", ", p.UserGroups.Select(u => u.Label)),
        });

        ListView = new ListViewModel<UsersListItemViewModel>(items, response.Result.TotalCount);

        Logger.LogInformation(
            "Displaying users list: Page {PageNumber}, PageSize {PageSize}, Total {TotalCount}, Search: {Search}",
            pageNumber,
            pageSize,
            response.Result.TotalCount,
            search
        );

        return Page();
    }

    /// <summary>
    /// View model for a single user in the list.
    /// </summary>
    public record UsersListItemViewModel
    {
        /// <summary>
        /// Gets the user's unique identifier.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the user's first name.
        /// </summary>
        [Display(Name = "First name")]
        public string FirstName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the user's last name.
        /// </summary>
        [Display(Name = "Last name")]
        public string LastName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the user's email address.
        /// </summary>
        [Display(Name = "Email address")]
        public string EmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the formatted creation time in the organization's time zone.
        /// </summary>
        [Display(Name = "Created at")]
        public string CreationTime { get; init; } = string.Empty;

        /// <summary>
        /// Gets the formatted last login time in the organization's time zone.
        /// </summary>
        [Display(Name = "Last logged in")]
        public string LastLoggedInTime { get; init; } = string.Empty;

        /// <summary>
        /// Gets whether the user is active (formatted as "Yes" or "No").
        /// </summary>
        [Display(Name = "Is active")]
        public string IsActive { get; init; } = string.Empty;

        /// <summary>
        /// Gets the comma-separated list of user group names.
        /// </summary>
        [Display(Name = "User groups")]
        public string UserGroups { get; init; } = string.Empty;
    }
}
