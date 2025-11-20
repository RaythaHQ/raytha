using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Utils;
using Raytha.Application.ContentTypes.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_CONTENT_TYPES_PERMISSION)]
public class Create : BaseAdminPageModel
{
    public string WebsiteUrl { get; set; }

    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Content Types",
                RouteName = RouteNames.Dashboard.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create content type",
                RouteName = RouteNames.ContentTypes.Create,
                IsActive = true,
            }
        );

        WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
        Form = new FormModel { DefaultRouteTemplate = "{ContentTypeDeveloperName}/{PrimaryField}" };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateContentType.Command
        {
            LabelPlural = Form.LabelPlural,
            LabelSingular = Form.LabelSingular,
            DefaultRouteTemplate = Form.DefaultRouteTemplate,
            Description = Form.Description,
            DeveloperName = Form.DeveloperName,
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"{Form.LabelPlural} edit successfully.");
            return RedirectToPage(
                RouteNames.ContentItems.Index,
                new { contentTypeDeveloperName = Form.DeveloperName.ToDeveloperName() }
            );
        }
        else
        {
            SetErrorMessage(
                "There were validation errors with your form submission. Please correct the fields below.",
                response.GetErrors()
            );
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + "/";
            return Page();
        }
    }

    public record FormModel
    {
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
    }
}
