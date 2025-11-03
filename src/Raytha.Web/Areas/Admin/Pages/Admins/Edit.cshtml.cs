using System.ComponentModel.DataAnnotations;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Roles.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.Admins;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Edit : BaseAdminPageModel, ISubActionViewModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public string Id { get; set; }
    public bool IsActive { get; set; }

    public async Task<IActionResult> OnGet(string id)
    {
        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false,
                Icon = SidebarIcons.Settings
            },
            new BreadcrumbNode
            {
                Label = "Admins",
                RouteName = RouteNames.Admins.Index,
                IsActive = false
            },
            new BreadcrumbNode
            {
                Label = "Edit",
                RouteName = RouteNames.Admins.Edit,
                IsActive = true
            }
        );

        var response = await Mediator.Send(new GetAdminById.Query { Id = id });

        var allRoles = await Mediator.Send(new GetRoles.Query());
        var userRoles = allRoles.Result.Items.Select(p => new FormModel.RoleCheckboxItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            IsSuperAdmin = BuiltInRole.SuperAdmin == p.DeveloperName,
            Selected = response.Result.Roles.Select(p => p.Id).Contains(p.Id),
        });

        Form = new FormModel
        {
            Roles = userRoles.ToArray(),
            Id = response.Result.Id,
            FirstName = response.Result.FirstName,
            LastName = response.Result.LastName,
            EmailAddress = response.Result.EmailAddress,
        };

        Id = id;
        IsActive = response.Result.IsActive;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditAdmin.Command
        {
            Id = Form.Id,
            FirstName = Form.FirstName,
            LastName = Form.LastName,
            EmailAddress = Form.EmailAddress,
            Roles = Form.Roles.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.FirstName} {Form.LastName} was updated successfully.");
            return RedirectToPage(RouteNames.Admins.Edit, new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this admin. See the error below.",
                response.GetErrors()
            );
            var admin = await Mediator.Send(new GetAdminById.Query { Id = id });
            Id = id;
            IsActive = admin.Result.IsActive;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        public RoleCheckboxItem_ViewModel[] Roles { get; set; }

        public class RoleCheckboxItem_ViewModel
        {
            public string Id { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
            public bool IsSuperAdmin { get; set; }
        }
    }
}
