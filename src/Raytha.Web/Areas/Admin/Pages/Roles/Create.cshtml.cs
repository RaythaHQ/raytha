using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Roles.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Roles;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new FormModel.SystemPermissionCheckboxItemViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = false,
            }
        );

        var contentTypesResponse = await Mediator.Send(new GetContentTypes.Query());
        var contentTypePermissions =
            new List<FormModel.ContentTypePermissionCheckboxItemViewModel>();

        foreach (var contentTypePermission in BuiltInContentTypePermission.Permissions)
        {
            foreach (var contentType in contentTypesResponse.Result.Items)
            {
                contentTypePermissions.Add(
                    new FormModel.ContentTypePermissionCheckboxItemViewModel
                    {
                        DeveloperName = contentTypePermission.DeveloperName,
                        Label = contentTypePermission.Label,
                        Selected = false,
                        ContentTypeId = contentType.Id,
                        ContentTypeLabel = contentType.LabelPlural,
                    }
                );
            }
        }

        Form = new FormModel
        {
            SystemPermissions = systemPermissions.ToArray(),
            ContentTypePermissions = contentTypePermissions.ToArray(),
        };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var contentTypePermissionsDict = new Dictionary<string, IEnumerable<string>>();
        foreach (var contentPermGroup in Form.ContentTypePermissions.GroupBy(p => p.ContentTypeId))
        {
            var selectedContentPermissions = contentPermGroup
                .Where(p => p.Selected)
                .Select(p => p.DeveloperName);
            contentTypePermissionsDict.Add(contentPermGroup.Key, selectedContentPermissions);
        }

        var input = new CreateRole.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            SystemPermissions = Form
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
            ContentTypePermissions = contentTypePermissionsDict,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(RouteNames.Roles.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this role. See the error below.",
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

        public SystemPermissionCheckboxItemViewModel[] SystemPermissions { get; set; }

        public ContentTypePermissionCheckboxItemViewModel[] ContentTypePermissions { get; set; }

        public class SystemPermissionCheckboxItemViewModel
        {
            public string DeveloperName { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }

        public class ContentTypePermissionCheckboxItemViewModel
        {
            public string DeveloperName { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
            public string ContentTypeLabel { get; set; }
            public string ContentTypeId { get; set; }
        }
    }
}
