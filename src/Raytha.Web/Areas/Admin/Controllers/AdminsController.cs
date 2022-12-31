using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Common.Utils;
using Raytha.Application.Roles.Queries;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Admins;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class AdminsController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins", Name = "adminsindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"LastLoggedInTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetAdmins.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new AdminsListItem_ViewModel
        {
            Id = p.Id,  
            FirstName = p.FirstName,
            LastName = p.LastName,
            EmailAddress = p.EmailAddress,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.CreationTime),
            LastLoggedInTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.LastLoggedInTime),
            IsActive = p.IsActive.YesOrNo(),
            Roles = string.Join(", ", p.Roles.Select(p => p.Label))
        });

        var viewModel = new List_ViewModel<AdminsListItem_ViewModel>(items, response.Result.TotalCount);
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/create", Name = "adminscreate")]
    public async Task<IActionResult> Create()
    {
        var roleChoicesResponse = await Mediator.Send(new GetRoles.Query());
        var rolesViewModel = roleChoicesResponse.Result.Items.Select(p => new AdminsCreate_ViewModel.RoleCheckboxItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            Selected = false
        }).ToArray();

        var model = new AdminsCreate_ViewModel { Roles = rolesViewModel };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/create", Name = "adminscreate")]
    public async Task<IActionResult> Create(AdminsCreate_ViewModel model)
    {
        var input = new CreateAdmin.Command
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailAddress = model.EmailAddress,
            Roles = model.Roles.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
            SendEmail = model.SendEmail
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.FirstName} {model.LastName} was created successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error attempting to create this admin. See the error below.", response.GetErrors());
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/edit/{id}", Name = "adminsedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetAdminById.Query { Id = id });

        var allRoles = await Mediator.Send(new GetRoles.Query());
        var userRoles = allRoles.Result.Items.Select(p => new AdminsEdit_ViewModel.RoleCheckboxItem_ViewModel
        {
            Id = p.Id,
            Label = p.Label,
            IsSuperAdmin = BuiltInRole.SuperAdmin == p.DeveloperName,
            Selected = response.Result.Roles.Select(p => p.Id).Contains(p.Id)
        });

        var model = new AdminsEdit_ViewModel
        {
            Id = response.Result.Id,
            FirstName = response.Result.FirstName,
            LastName = response.Result.LastName,
            EmailAddress = response.Result.EmailAddress,
            Roles = userRoles.ToArray(),
            IsActive = response.Result.IsActive,
            CurrentUserId = CurrentUser.UserId,
            EmailAndPasswordEnabledForAdmins = CurrentOrganization.EmailAndPasswordIsEnabledForAdmins
        };
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/edit/{id}", Name = "adminsedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminsEdit_ViewModel model, string id)
    {
        var input = new EditAdmin.Command
        {
            Id = id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailAddress = model.EmailAddress,
            Roles = model.Roles.Where(p => p.Selected).Select(p => (ShortGuid)p.Id)
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.FirstName} {model.LastName} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        else
        {
            SetErrorMessage("There was an error attempting to update this admin. See the error below.", response.GetErrors());
            var admin = await Mediator.Send(new GetUserById.Query { Id = id });
            model.Id = id;
            model.EmailAndPasswordEnabledForAdmins = CurrentOrganization.EmailAndPasswordIsEnabledForAdmins;
            model.CurrentUserId = CurrentUser.UserId;
            model.IsActive = admin.Result.IsActive;

            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/reset-password/{id}", Name = "adminsresetpassword")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForAdmins)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToAction("Edit", new { id });
        }

        var response = await Mediator.Send(new GetAdminById.Query { Id = id });

        var model = new AdminsResetPassword_ViewModel
        {
            CurrentUserId = CurrentUser.UserId,
            Id = id,
            IsActive = response.Result.IsActive,
            EmailAndPasswordEnabledForAdmins = CurrentOrganization.EmailAndPasswordIsEnabledForAdmins
        };
        
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/reset-password/{id}", Name = "adminsresetpassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminsResetPassword_ViewModel model, string id)
    {
        var input = new ResetPassword.Command
        {
            Id = id,
            ConfirmNewPassword = model.ConfirmNewPassword,
            NewPassword = model.NewPassword,
            SendEmail = model.SendEmail
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Password was reset successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error attempting to reset this password. See the error below.", response.GetErrors());
            var admin = await Mediator.Send(new GetAdminById.Query { Id = id });
            model.Id = id;
            model.IsActive = admin.Result.IsActive;
            model.EmailAndPasswordEnabledForAdmins = CurrentOrganization.EmailAndPasswordIsEnabledForAdmins;
            model.CurrentUserId = CurrentUser.UserId;

            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/suspend/{id}", Name = "adminssuspend")]
    [HttpPost]
    public async Task<IActionResult> Suspend(string id)
    {
        var input = new SetIsActive.Command { Id = id, IsActive = false };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            SetSuccessMessage($"Account has been suspended.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToAction("Edit", new { id });
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/restore/{id}", Name = "adminsrestore")]
    [HttpPost]
    public async Task<IActionResult> Restore(string id)
    {
        var response = await Mediator.Send(new SetIsActive.Command { Id = id, IsActive = true });
        if (response.Success)
        {
            SetSuccessMessage($"Account has been restored.");
        }
        else
        {
            SetErrorMessage(response.Error);
        }

        return RedirectToAction("Edit", new { id });
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/remove-access/{id}", Name = "adminsremoveaccess")]
    [HttpPost]
    public async Task<IActionResult> RemoveAccess(string id)
    {
        var response = await Mediator.Send(new RemoveAdminAccess.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Administrator access has been removed.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToAction("Index");
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/settings/admins/delete/{id}", Name = "adminsdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await Mediator.Send(new DeleteAdmin.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Administrator has been deleted.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
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
