using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Common.Utils;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.AdminsListItemViewModel>
{
    public ListViewModel<AdminsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"LastLoggedInTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetAdmins.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new AdminsListItemViewModel
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
            Roles = string.Join(", ", p.Roles.Select(p => p.Label)),
        });

        ListView = new ListViewModel<AdminsListItemViewModel>(items, response.Result.TotalCount);
        return Page();
    }

    public record AdminsListItemViewModel
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

        [Display(Name = "Roles")]
        public string Roles { get; init; }
    }
}
