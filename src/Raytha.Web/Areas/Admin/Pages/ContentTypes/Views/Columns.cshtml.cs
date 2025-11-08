using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Columns : BaseContentTypeContextPageModel
{
    public string BackToListUrl { get; set; } = string.Empty;

    public ColumnListItemViewModel[] SelectedColumns { get; set; } =
        Array.Empty<ColumnListItemViewModel>();
    public ColumnListItemViewModel[] NotSelectedColumns { get; set; } =
        Array.Empty<ColumnListItemViewModel>();

    public async Task<IActionResult> OnGet(string viewId, string backToListUrl = "")
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView!.ContentType.LabelPlural,
                RouteName = RouteNames.ContentItems.Index,
                RouteValues = new Dictionary<string, string>
                {
                    { "contentTypeDeveloperName", CurrentView.ContentType.DeveloperName },
                },
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Views",
                RouteName = RouteNames.ContentTypes.Views.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Columns",
                RouteName = RouteNames.ContentTypes.Views.Columns,
                IsActive = true,
            }
        );

        await LoadColumns();
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(
        string viewId,
        string developerName,
        string backToListUrl = ""
    )
    {
        var action = Request.Form["action"].ToString();
        var command = new EditColumn.Command
        {
            Id = viewId,
            DeveloperName = developerName,
            ShowColumn = action == "add",
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            SetErrorMessage(
                "There was an error adding this column to the view",
                response.GetErrors()
            );
        }

        return RedirectToPage(
            RouteNames.ContentTypes.Views.Columns,
            new
            {
                contentTypeDeveloperName = CurrentView!.ContentType.DeveloperName,
                viewId = viewId,
                backToListUrl = backToListUrl,
            }
        );
    }

    public async Task<IActionResult> OnPostAjaxAsync(string viewId, string developerName)
    {
        var position = Request.Form["position"];
        var input = new ReorderColumn.Command
        {
            Id = viewId,
            DeveloperName = developerName,
            NewFieldOrder = Convert.ToInt32(position),
        };

        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return new JsonResult(response);
    }

    private async Task LoadColumns()
    {
        var response = await Mediator.Send(
            new GetContentTypeFields.Query
            {
                PageSize = int.MaxValue,
                OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
                DeveloperName = CurrentView!.ContentType.DeveloperName,
            }
        );

        var columnListItems = response
            .Result!.Items.Select(p => new ColumnListItemViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Columns.Contains(p.DeveloperName),
                FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName),
            })
            .ToList();

        var builtInListItems = BuiltInContentTypeField
            .ReservedContentTypeFields.Select(p => new ColumnListItemViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Columns.Contains(p.DeveloperName),
                FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName),
            })
            .ToList();

        columnListItems.AddRange(builtInListItems);

        var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
        var notSelectedColumns = columnListItems
            .Where(p => !p.Selected)
            .OrderBy(c => c.DeveloperName);

        SelectedColumns = selectedColumns.ToArray();
        NotSelectedColumns = notSelectedColumns.ToArray();
    }

    public record ColumnListItemViewModel
    {
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public bool Selected { get; init; }
        public int FieldOrder { get; init; }
    }
}
