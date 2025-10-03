using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.RaythaFunctions;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Revisions : BaseAdminPageModel, ISubActionViewModel
{
    public RaythaFunctionsRevisionsPaginationViewModel ListView { get; set; }
    public string Id { get; set; }
    public bool IsActive { get; set; }

    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGet(
        string id,
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        Id = id;
        var input = new GetRaythaFunctionRevisionsByRaythaFunctionId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(
            rfr => new RaythaFunctionsRevisionsListItemViewModel
            {
                Id = rfr.Id,
                Code = rfr.Code,
                CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    rfr.CreationTime
                ),
                CreatorUser = rfr.CreatorUser?.FullName ?? "N/A",
            }
        );

        ListView = new RaythaFunctionsRevisionsPaginationViewModel(
            items,
            response.Result.TotalCount
        )
        {
            FunctionId = id,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostRevert(string id, string revisionId)
    {
        Id = id;
        var input = new RevertRaythaFunction.Command { Id = revisionId };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Function has been reverted.");
            return RedirectToPage("/RaythaFunctions/Edit", new { id });
        }
        else
        {
            SetErrorMessage("There was an error reverting this function", response.GetErrors());
            return RedirectToPage("/RaythaFunctions/Revisions", new { id });
        }
    }

    public record RaythaFunctionsRevisionsPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<RaythaFunctionsRevisionsListItemViewModel> Items { get; }
        public string FunctionId { get; set; }

        public RaythaFunctionsRevisionsPaginationViewModel(
            IEnumerable<RaythaFunctionsRevisionsListItemViewModel> items,
            int totalCount
        )
            : base(totalCount) => Items = items;
    }

    public record RaythaFunctionsRevisionsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Created at")]
        public string CreationTime { get; init; }

        [Display(Name = "Created by")]
        public string CreatorUser { get; init; }

        [Display(Name = "Code")]
        public string Code { get; init; }
    }
}
