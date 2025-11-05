using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Views.Commands;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
public class Edit : BaseContentTypeContextPageModel
{
    public string BackToListUrl { get; set; } = string.Empty;

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id, string backToListUrl = "")
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView.ContentType.LabelPlural,
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
                Label = "Edit",
                RouteName = RouteNames.ContentTypes.Views.Edit,
                IsActive = true,
            }
        );

        var view = await Mediator.Send(new GetViewById.Query { Id = id });
        Form = new FormModel
        {
            Label = view.Result.Label,
            DeveloperName = view.Result.DeveloperName,
            Description = view.Result.Description,
        };
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditView.Command
        {
            Id = id,
            Label = Form.Label,
            Description = Form.Description,
        };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was editedsuccessfully.");
            return RedirectToPage(
                RouteNames.ContentItems.Index,
                new
                {
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                    viewId = id,
                }
            );
        }
        else
        {
            SetErrorMessage(
                "There were validation errors with your form submission. Please correct the fields below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        public string Description { get; set; }
    }
}
