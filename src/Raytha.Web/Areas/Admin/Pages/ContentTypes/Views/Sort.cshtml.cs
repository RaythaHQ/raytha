using System.ComponentModel.DataAnnotations;
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
public class Sort : BaseContentTypeContextPageModel
{
    public string BackToListUrl { get; set; } = string.Empty;

    public SortColumnListItemViewModel[] SelectedColumns { get; set; } =
        Array.Empty<SortColumnListItemViewModel>();
    public Dictionary<string, string> NotSelectedColumns { get; set; } = new();

    [BindProperty]
    public string DeveloperName { get; set; } = string.Empty;

    [BindProperty]
    public string OrderByDirection { get; set; } = SortOrder.ASCENDING;

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
                Icon = SidebarIcons.ContentItems,
            },
            new BreadcrumbNode
            {
                Label = "Views",
                RouteName = RouteNames.ContentTypes.Views.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Edit sort",
                RouteName = RouteNames.ContentTypes.Views.Sort,
                IsActive = true,
            }
        );

        await LoadSortColumns();
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(
        string viewId,
        string developerName,
        string backToListUrl = ""
    )
    {
        var command = new EditSort.Command
        {
            Id = viewId,
            DeveloperName = developerName,
            ShowColumn = false,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            SetErrorMessage(
                "There was an error removing this column from the sort",
                response.GetErrors()
            );
        }

        return RedirectToPage(
            RouteNames.ContentTypes.Views.Sort,
            new
            {
                contentTypeDeveloperName = CurrentView!.ContentType.DeveloperName,
                viewId = viewId,
                backToListUrl = backToListUrl,
            }
        );
    }

    public async Task<IActionResult> OnPostAddAsync(string viewId, string backToListUrl = "")
    {
        var command = new EditSort.Command
        {
            Id = viewId,
            DeveloperName = DeveloperName,
            ShowColumn = true,
            OrderByDirection = OrderByDirection,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            SetErrorMessage(
                "There was an error adding this column to the sort",
                response.GetErrors()
            );
        }

        return RedirectToPage(
            RouteNames.ContentTypes.Views.Sort,
            new
            {
                contentTypeDeveloperName = CurrentView!.ContentType.DeveloperName,
                viewId = viewId,
                backToListUrl = backToListUrl,
            }
        );
    }

    public async Task<IActionResult> OnPostAjaxAsync(string id, string developerName)
    {
        var position = Request.Form["position"];
        var input = new ReorderSort.Command
        {
            Id = id,
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

    private async Task LoadSortColumns()
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
            .Result!.Items.Select(p => new SortColumnListItemViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
                FieldOrder = CurrentView
                    .Sort.Select(x => x.DeveloperName)
                    .ToList()
                    .IndexOf(p.DeveloperName),
                OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName)
                    ? CurrentView
                        .Sort.First(c => c.DeveloperName == p.DeveloperName)
                        .SortOrder!.DeveloperName
                    : SortOrder.ASCENDING,
            })
            .ToList();

        var builtInListItems = BuiltInContentTypeField
            .ReservedContentTypeFields.Select(p => new SortColumnListItemViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
                FieldOrder = CurrentView
                    .Sort.Select(x => x.DeveloperName)
                    .ToList()
                    .IndexOf(p.DeveloperName),
                OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName)
                    ? CurrentView
                        .Sort.First(c => c.DeveloperName == p.DeveloperName)
                        .SortOrder!.DeveloperName
                    : SortOrder.ASCENDING,
            })
            .ToList();

        columnListItems.AddRange(builtInListItems);

        var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
        var notSelectedColumns = columnListItems
            .Where(p => !p.Selected)
            .OrderBy(c => c.DeveloperName);

        SelectedColumns = selectedColumns.ToArray();
        NotSelectedColumns = notSelectedColumns.ToDictionary(p => p.DeveloperName, p => p.Label);
    }

    public record SortColumnListItemViewModel
    {
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public bool Selected { get; init; }
        public int FieldOrder { get; init; }
        public string OrderByDirection { get; init; } = SortOrder.ASCENDING;
    }
}
