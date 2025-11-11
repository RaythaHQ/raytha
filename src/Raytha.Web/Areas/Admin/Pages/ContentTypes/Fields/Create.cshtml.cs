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
public class Create : BaseContentTypeContextPageModel
{
    public Dictionary<string, string> AvailableContentTypes { get; set; }
    public Dictionary<string, string> AvailableFieldTypes { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
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
                Label = "Fields",
                RouteName = RouteNames.ContentTypes.Fields.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create",
                RouteName = RouteNames.ContentTypes.Fields.Create,
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
        Form = new FormModel { ContentTypeId = CurrentView.ContentTypeId };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateContentTypeField.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            ContentTypeId = Form.ContentTypeId,
            FieldType = Form.FieldType,
            Choices =
                Form.Choices?.Select(p => new ContentTypeFieldChoiceInputDto
                {
                    Label = p.Label,
                    DeveloperName = p.DeveloperName,
                    Disabled = p.Disabled,
                }) ?? Enumerable.Empty<ContentTypeFieldChoiceInputDto>(),
            IsRequired = Form.IsRequired,
            Description = Form.Description,
            RelatedContentTypeId = Form.RelatedContentTypeId,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(
                RouteNames.ContentTypes.Fields.Index,
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
            );
        }
        else
        {
            var contentTypes = await Mediator.Send(new GetContentTypes.Query());
            SetErrorMessage(
                "There was an error attempting to save this field definition. See the error below.",
                response.GetErrors()
            );

            AvailableContentTypes = contentTypes.Result.Items.ToDictionary(
                p => p.Id.ToString(),
                p => p.LabelPlural
            );
            AvailableFieldTypes = BaseFieldType.SupportedTypes.ToDictionary(
                p => p.DeveloperName,
                p => p.Label
            );
            return Page();
        }
    }

    public record FormModel
    {
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

        public FieldChoiceItemViewModel[] Choices { get; set; } =
            [
                new FieldChoiceItemViewModel
                {
                    Label = "Choice label 1",
                    DeveloperName = "developer_name_1",
                    Disabled = false,
                },
                new FieldChoiceItemViewModel
                {
                    Label = "Choice label 2",
                    DeveloperName = "developer_name_2",
                    Disabled = false,
                },
                new FieldChoiceItemViewModel
                {
                    Label = "Choice label 3",
                    DeveloperName = "developer_name_3",
                    Disabled = false,
                },
            ];
    }

    public record FieldChoiceItemViewModel
    {
        public string Label { get; set; }
        public string DeveloperName { get; set; }
        public string Value { get; set; }
        public bool Disabled { get; set; }
    }
}
