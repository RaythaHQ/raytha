using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Fields;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class Reorder : BaseContentTypeContextPageModel
{
    public FieldsPaginationViewModel ListView { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var input = new GetContentTypeFields.Query
        {
            PageSize = int.MaxValue,
            OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
            DeveloperName = CurrentView.ContentType.DeveloperName,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new FieldsListItemViewModel
        {
            Id = p.Id,
            Label = p.Label,
            DeveloperName = p.DeveloperName,
            FieldType = p.FieldType,
            IsRequired = p.IsRequired.YesOrNo(),
        });

        ListView = new FieldsPaginationViewModel(items, response.Result.TotalCount, false);

        return Page();
    }

    public async Task<IActionResult> Ajax(string id)
    {
        var position = Request.Form["position"];
        var input = new ReorderContentTypeField.Command
        {
            Id = id,
            NewFieldOrder = Convert.ToInt32(position),
        };

        var result = await Mediator.Send(input);
        if (result.Success)
            return new JsonResult(result);
        else
            return new JsonResult(result, new { StatusCode = 400 });
    }

    public record FieldsPaginationViewModel : PaginationViewModel
    {
        public IEnumerable<FieldsListItemViewModel> Items { get; }
        public bool ShowDeletedOnly { get; }

        public FieldsPaginationViewModel(
            IEnumerable<FieldsListItemViewModel> items,
            int totalCount,
            bool showDeletedOnly
        )
            : base(totalCount)
        {
            Items = items;
            ShowDeletedOnly = showDeletedOnly;
        }
    }

    public record FieldsListItemViewModel
    {
        public string Id { get; init; }

        [Display(Name = "Label")]
        public string Label { get; init; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; init; }

        [Display(Name = "Field type")]
        public string FieldType { get; init; }

        [Display(Name = "Is required")]
        public string IsRequired { get; init; }
    }
}
