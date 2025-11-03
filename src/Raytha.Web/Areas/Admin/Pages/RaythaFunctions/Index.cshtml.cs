using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel, IHasListView<Index.RaythaFunctionsListItemViewModel>
{
    public ListViewModel<RaythaFunctionsListItemViewModel> ListView { get; set; }

    public async Task<IActionResult> OnGet(
        string search = "",
        string orderBy = $"Label {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Functions",
                RouteName = RouteNames.RaythaFunctions.Index,
                IsActive = true,
                Icon = SidebarIcons.Functions
            }
        );

        var input = new GetRaythaFunctions.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(rf => new RaythaFunctionsListItemViewModel
        {
            Id = rf.Id,
            Name = rf.Name,
            DeveloperName = rf.DeveloperName,
            TriggerType = rf.TriggerType.Label,
            IsActive = rf.IsActive.YesOrNo(),
            LastModificationTime =
                CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    rf.LastModificationTime
                ),
            LastModifierUser = rf.LastModifierUser?.FullName ?? "N/A",
        });

        ListView = new ListViewModel<RaythaFunctionsListItemViewModel>(
            items,
            response.Result.TotalCount
        );
        return Page();
    }

    public record RaythaFunctionsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Name")]
        public string Name { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Trigger type")]
        public string TriggerType { get; set; }

        [Display(Name = "Active")]
        public string IsActive { get; set; }

        [Display(Name = "Last modified at")]
        public string LastModificationTime { get; init; }

        [Display(Name = "Last modified by")]
        public string LastModifierUser { get; init; }
    }
}
