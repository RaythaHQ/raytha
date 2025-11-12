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
public class Create : BaseContentTypeContextPageModel
{
    public string BackToListUrl { get; set; } = string.Empty;

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string duplicateFromId = null, string backToListUrl = "")
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
                Label = "Create",
                RouteName = RouteNames.ContentTypes.Views.Create,
                IsActive = true,
            }
        );

        if (duplicateFromId != null)
        {
            var view = await Mediator.Send(new GetViewById.Query { Id = duplicateFromId });
            Form = new FormModel
            {
                Label = view.Result.Label,
                DeveloperName = view.Result.DeveloperName,
                Description = view.Result.Description,
                ContentTypeId = view.Result.ContentTypeId,
                DuplicateFromId = duplicateFromId,
            };
        }
        else
        {
            Form = new FormModel { ContentTypeId = CurrentView.ContentTypeId };
        }
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateView.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            ContentTypeId = Form.ContentTypeId,
            DuplicateFromId = Form.DuplicateFromId,
            Description = Form.Description,
        };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(
                RouteNames.ContentItems.Index,
                new
                {
                    contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                    viewId = response.Result,
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

        public string ContentTypeId { get; set; }

        public string DuplicateFromId { get; set; }
    }
}
