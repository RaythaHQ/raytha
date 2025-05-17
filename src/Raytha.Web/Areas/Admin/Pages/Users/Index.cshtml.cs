using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Users;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.UsersListItemViewModel>
{
    public ListViewModel<UsersListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"LastLoggedInTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetUsers.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

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
            UserGroups = string.Join(", ", p.UserGroups.Select(p => p.Label)),
        });

        ListView = new ListViewModel<UsersListItemViewModel>(items, response.Result.TotalCount);
        return Page();
    }

    public record UsersListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "First name")]
        public string FirstName { get; init; }

        [Display(Name = "Last name")]
        public string LastName { get; init; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; init; }

        [Display(Name = "Created at")]
        public string CreationTime { get; init; }

        [Display(Name = "Last logged in")]
        public string LastLoggedInTime { get; init; }

        [Display(Name = "Is active")]
        public string IsActive { get; init; }

        [Display(Name = "User groups")]
        public string UserGroups { get; init; }
    }
}
