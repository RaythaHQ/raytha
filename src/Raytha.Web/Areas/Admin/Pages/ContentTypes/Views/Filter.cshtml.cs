using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Filter : BaseContentTypeContextPageModel
{
    public string BackToListUrl { get; set; } = string.Empty;

    public ViewsFilterContentTypeField_ViewModel[] ContentTypeFields { get; set; } =
        Array.Empty<ViewsFilterContentTypeField_ViewModel>();
    public string ContentTypeFieldsJson { get; set; } = "[]";

    public bool HasExistingFilter => CurrentView!.Filter != null && CurrentView.Filter.Any();
    public FilterCondition? RootFilterCondition =>
        CurrentView!.Filter?.FirstOrDefault(p => p.ParentId == null);

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
                Label = "Filter",
                RouteName = RouteNames.ContentTypes.Views.Filter,
                IsActive = true,
            }
        );

        await LoadFilterFields();
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(string viewId, [FromForm] string json)
    {
        IEnumerable<FilterConditionInputDto> items;
        try
        {
            items =
                JsonSerializer.Deserialize<IEnumerable<FilterConditionInputDto>>(json)
                ?? Enumerable.Empty<FilterConditionInputDto>();
        }
        catch
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return new JsonResult(new { success = false, error = "Invalid JSON." });
        }

        var response = await Mediator.Send(
            new EditFilter.Command { Id = CurrentView!.Id, Filter = items }
        );

        if (!response.Success)
            Response.StatusCode = StatusCodes.Status400BadRequest;

        return new JsonResult(response);
    }

    private async Task LoadFilterFields()
    {
        var response = await Mediator.Send(
            new GetContentTypeFields.Query
            {
                PageSize = int.MaxValue,
                OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
                DeveloperName = CurrentView!.ContentType.DeveloperName,
            }
        );

        var contentTypeFields = response
            .Result!.Items.Select(p => new ViewsFilterContentTypeField_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                FieldType = p.FieldType,
                Choices =
                    p.Choices?.Select(x => x.DeveloperName).ToArray() ?? Array.Empty<string>(),
            })
            .ToList();

        var builtIns = BuiltInContentTypeField
            .ReservedContentTypeFields.Where(p =>
                p.DeveloperName != BuiltInContentTypeField.CreatorUser
                && p.DeveloperName != BuiltInContentTypeField.LastModifierUser
            )
            .Select(p => new ViewsFilterContentTypeField_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                FieldType = p.FieldType,
            })
            .ToList();

        contentTypeFields.AddRange(builtIns);

        ContentTypeFields = contentTypeFields.ToArray();
        ContentTypeFieldsJson = JsonSerializer.Serialize(ContentTypeFields);
    }

    public record ViewsFilterContentTypeField_ViewModel
    {
        public string Label { get; init; } = string.Empty;
        public string DeveloperName { get; init; } = string.Empty;
        public BaseFieldType? FieldType { get; init; }
        public string[] Choices { get; init; } = Array.Empty<string>();
    }

    public record FilterSubtree_ViewModel
    {
        public FilterCondition FilterCondition { get; init; } = null!;
        public ViewsFilterContentTypeField_ViewModel[] ContentTypeFields { get; init; } =
            Array.Empty<ViewsFilterContentTypeField_ViewModel>();
        public IEnumerable<FilterCondition> AllFilterConditions { get; init; } =
            Array.Empty<FilterCondition>();
    }
}
