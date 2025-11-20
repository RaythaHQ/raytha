using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Fields;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class Edit : BaseContentTypeContextPageModel, ISubActionViewModel
{
    public string Id { get; set; }
    public Dictionary<string, string> AvailableContentTypes { get; set; }
    public Dictionary<string, string> AvailableFieldTypes { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        var response = await Mediator.Send(new GetContentTypeFieldById.Query { Id = id });

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
                Label = "Fields",
                RouteName = RouteNames.ContentTypes.Fields.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Edit field",
                RouteName = RouteNames.ContentTypes.Fields.Edit,
                IsActive = true,
            }
        );
        var contentTypes = await Mediator.Send(new GetContentTypes.Query());
        AvailableContentTypes = contentTypes.Result.Items.ToDictionary(
            p => p.Id.ToString(),
            p => p.LabelPlural
        );
        AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(
            p => p.DeveloperName,
            p => p.Label
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            FieldType = response.Result.FieldType,
            DeveloperName = response.Result.DeveloperName,
            Choices = response
                .Result.Choices?.Select(p => new FieldChoiceItemViewModel
                {
                    Label = p.Label,
                    DeveloperName = p.DeveloperName,
                    Disabled = p.Disabled,
                })
                .ToArray(),
            IsRequired = response.Result.IsRequired,
            Description = response.Result.Description,
            RelatedContentTypeId = response.Result.RelatedContentTypeId,
        };
        Id = id;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditContentTypeField.Command
        {
            Id = id,
            Label = Form.Label,
            Choices =
                Form.Choices?.Select(p => new ContentTypeFieldChoiceInputDto
                {
                    Label = p.Label,
                    DeveloperName = p.DeveloperName,
                    Disabled = p.Disabled,
                }) ?? Enumerable.Empty<ContentTypeFieldChoiceInputDto>(),
            IsRequired = Form.IsRequired,
            Description = Form.Description,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was edited successfully.");
            return RedirectToPage(
                RouteNames.ContentTypes.Fields.Index,
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save this field definition. See the error below.",
                response.GetErrors()
            );

            var contentTypes = await Mediator.Send(new GetContentTypes.Query());
            AvailableContentTypes = contentTypes.Result.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.LabelPlural
            );
            AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(
                p => p.DeveloperName,
                p => p.Label
            );
            Id = id;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        public string ContentTypeId { get; set; }

        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Field type")]
        public string FieldType { get; set; }

        [Display(Name = "Content type to establish a relationship with")]
        public string RelatedContentTypeId { get; set; }

        [Display(Name = "Is a required field")]
        public bool IsRequired { get; set; } = true;

        [Display(Name = "Instructions for this field")]
        public string Description { get; set; }

        public FieldChoiceItemViewModel[] Choices { get; set; }
    }

    public record FieldChoiceItemViewModel
    {
        public string Label { get; set; }
        public string DeveloperName { get; set; }
        public string Value { get; set; }
        public bool Disabled { get; set; }
    }
}
