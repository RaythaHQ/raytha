using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Roles.Commands;
using Raytha.Application.Roles.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Roles;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class RolesController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles", Name = "rolesindex")]
    public async Task<IActionResult> Index(
        string search = "",
        string orderBy = $"Label {SortOrder.ASCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetRoles.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new RolesListItem_ViewModel
        {
            DeveloperName = p.DeveloperName,
            Label = p.Label,
            Id = p.Id,
        });

        var viewModel = new List_ViewModel<RolesListItem_ViewModel>(
            items,
            response.Result.TotalCount
        );
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles/create", Name = "rolescreate")]
    public async Task<IActionResult> Create()
    {
        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new CreateRole_ViewModel.SystemPermissionCheckboxItem_ViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = false,
            }
        );

        var contentTypesResponse = await Mediator.Send(new GetContentTypes.Query());
        var contentTypePermissions =
            new List<CreateRole_ViewModel.ContentTypePermissionCheckboxItem_ViewModel>();
        foreach (var contentTypePermission in BuiltInContentTypePermission.Permissions)
        {
            foreach (var contentType in contentTypesResponse.Result.Items)
            {
                contentTypePermissions.Add(
                    new CreateRole_ViewModel.ContentTypePermissionCheckboxItem_ViewModel
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

        var model = new CreateRole_ViewModel
        {
            SystemPermissions = systemPermissions.ToArray(),
            ContentTypePermissions = contentTypePermissions.ToArray(),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles/create", Name = "rolescreate")]
    public async Task<IActionResult> Create(CreateRole_ViewModel model)
    {
        var contentTypePermissionsDict = new Dictionary<string, IEnumerable<string>>();
        foreach (var contentPermGroup in model.ContentTypePermissions.GroupBy(p => p.ContentTypeId))
        {
            var selectedContentPermissions = contentPermGroup
                .Where(p => p.Selected)
                .Select(p => p.DeveloperName);
            contentTypePermissionsDict.Add(contentPermGroup.Key, selectedContentPermissions);
        }

        var input = new CreateRole.Command
        {
            Label = model.Label,
            DeveloperName = model.DeveloperName,
            SystemPermissions = model
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
            ContentTypePermissions = contentTypePermissionsDict,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this role. See the error below.",
                response.GetErrors()
            );
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles/edit/{id}", Name = "rolesedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetRoleById.Query { Id = id });

        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new EditRole_ViewModel.SystemPermissionCheckboxItem_ViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = response.Result.SystemPermissions.Any(c => c == p.DeveloperName),
            }
        );

        var contentTypesResponse = await Mediator.Send(new GetContentTypes.Query());
        var contentTypePermissions =
            new List<EditRole_ViewModel.ContentTypePermissionCheckboxItem_ViewModel>();
        foreach (var contentTypePermission in BuiltInContentTypePermission.Permissions)
        {
            foreach (var contentType in contentTypesResponse.Result.Items)
            {
                contentTypePermissions.Add(
                    new EditRole_ViewModel.ContentTypePermissionCheckboxItem_ViewModel
                    {
                        DeveloperName = contentTypePermission.DeveloperName,
                        Label = contentTypePermission.Label,
                        Selected = response.Result.ContentTypePermissions.Any(c =>
                            c.Key == contentType.Id
                            && c.Value.Any(p => p == contentTypePermission.DeveloperName)
                        ),
                        ContentTypeId = contentType.Id,
                        ContentTypeLabel = contentType.LabelPlural,
                    }
                );
            }
        }

        var model = new EditRole_ViewModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            SystemPermissions = systemPermissions.ToArray(),
            ContentTypePermissions = contentTypePermissions.ToArray(),
            IsSuperAdmin = BuiltInRole.SuperAdmin.DeveloperName == response.Result.DeveloperName,
        };
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles/edit/{id}", Name = "rolesedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditRole_ViewModel model, string id)
    {
        var contentTypePermissionsDict = new Dictionary<string, IEnumerable<string>>();
        foreach (var contentPermGroup in model.ContentTypePermissions.GroupBy(p => p.ContentTypeId))
        {
            var selectedContentPermissions = contentPermGroup
                .Where(p => p.Selected)
                .Select(p => p.DeveloperName);
            contentTypePermissionsDict.Add(contentPermGroup.Key, selectedContentPermissions);
        }

        var input = new EditRole.Command
        {
            Id = id,
            Label = model.Label,
            SystemPermissions = model
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
            ContentTypePermissions = contentTypePermissionsDict,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        {
            SetErrorMessage(
                "There was an error attempting to update this role. See the error below.",
                response.GetErrors()
            );
            model.Id = id;
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/roles/delete/{id}", Name = "rolesdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await Mediator.Send(new DeleteRole.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Role has been deleted.");
        }
        else
        {
            SetErrorMessage("There was a problem deleting this role", response.GetErrors());
        }

        return RedirectToAction("Index");
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Admins";
        ViewData["ExpandSettingsMenu"] = true;
    }
}
