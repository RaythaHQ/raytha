using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Roles.Commands;
using Raytha.Domain.Entities;

namespace Raytha.Web.Areas.Admin.Pages.Roles;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new FormModel.SystemPermissionCheckboxItem_ViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = false,
            }
        );

        Form = new FormModel { SystemPermissions = systemPermissions.ToArray() };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateRole.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            SystemPermissions = Form
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage("/Roles/Index");
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

        public SystemPermissionCheckboxItem_ViewModel[] SystemPermissions { get; set; }

        public class SystemPermissionCheckboxItem_ViewModel
        {
            public string DeveloperName { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }
    }
}
