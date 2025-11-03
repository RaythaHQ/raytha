using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects.FieldTypes;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_CONFIG_PERMISSION)]
public class Configuration : BaseContentTypeContextPageModel
{
    public string WebsiteUrl { get; set; }
    public Dictionary<string, string> ContentTypeFields { get; set; }

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
                Label = "Configuration",
                RouteName = RouteNames.ContentTypes.Configuration,
                IsActive = true,
            }
        );

        WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
        ContentTypeFields = CurrentView
            .ContentType.ContentTypeFields.Where(p =>
                p.FieldType.DeveloperName == BaseFieldType.SingleLineText
            )
            .ToDictionary(p => p.Id.ToString(), p => p.DeveloperName);

        Form = new FormModel
        {
            LabelPlural = CurrentView.ContentType.LabelPlural,
            LabelSingular = CurrentView.ContentType.LabelSingular,
            DeveloperName = CurrentView.ContentType.DeveloperName,
            DefaultRouteTemplate = CurrentView.ContentType.DefaultRouteTemplate,
            Description = CurrentView.ContentType.Description,
            Id = CurrentView.ContentTypeId,
            PrimaryFieldId = CurrentView.ContentType.PrimaryFieldId,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new EditContentType.Command
        {
            LabelPlural = Form.LabelPlural,
            LabelSingular = Form.LabelSingular,
            DefaultRouteTemplate = Form.DefaultRouteTemplate,
            Id = CurrentView.ContentTypeId,
            Description = Form.Description,
            PrimaryFieldId = Form.PrimaryFieldId,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{Form.LabelPlural} edit successfully.");
            return RedirectToPage(RouteNames.ContentTypes.Configuration);
        }
        else
        {
            SetErrorMessage(
                "There were validation errors with your form submission. Please correct the fields below.",
                response.GetErrors()
            );
            ContentTypeFields = CurrentView
                .ContentType.ContentTypeFields.Where(p =>
                    p.FieldType.DeveloperName == BaseFieldType.SingleLineText
                )
                .ToDictionary(p => p.Id.ToString(), p => p.DeveloperName);
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "Label (plural)")]
        public string LabelPlural { get; set; }

        [Display(Name = "Label (singular)")]
        public string LabelSingular { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Default route template")]
        public string DefaultRouteTemplate { get; set; }

        [Display(Name = "Primary field")]
        public string PrimaryFieldId { get; set; }
    }
}
