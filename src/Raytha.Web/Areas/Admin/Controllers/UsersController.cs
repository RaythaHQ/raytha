using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Common.Utils;
using Raytha.Application.UserGroups.Queries;
using Raytha.Application.Users.Commands;
using Raytha.Application.Users.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Areas.Admin.Views.Users;
using Raytha.Web.Filters;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class UsersController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/users", Name = "usersindex")]
    public async Task<IActionResult> Index(
        string search = "",
        string orderBy = $"LastLoggedInTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50
    )
    {
        var input = new GetUsers.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(p => new UsersListItem_ViewModel
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            EmailAddress = p.EmailAddress,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            LastLoggedInTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.LastLoggedInTime
            ),
            IsActive = p.IsActive.YesOrNo(),
            UserGroups = string.Join(", ", p.UserGroups.Select(p => p.Label)),
        });

        var viewModel = new List_ViewModel<UsersListItem_ViewModel>(
            items,
            response.Result.TotalCount
        );
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/create", Name = "userscreate")]
    public async Task<IActionResult> Create()
    {
        var userGroupsChoicesResponse = await Mediator.Send(new GetUserGroups.Query());
        var userGroupsViewModel = userGroupsChoicesResponse
            .Result.Items.Select(p => new UsersCreate_ViewModel.UserGroupCheckboxItem_ViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = false,
            })
            .ToArray();

        var model = new UsersCreate_ViewModel { UserGroups = userGroupsViewModel };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(RAYTHA_ROUTE_PREFIX + "/users/create", Name = "userscreate")]
    public async Task<IActionResult> Create(UsersCreate_ViewModel model)
    {
        var input = new CreateUser.Command
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailAddress = model.EmailAddress,
            UserGroups = model.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
            SendEmail = model.SendEmail,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.FirstName} {model.LastName} was created successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this user. See the error below.",
                response.GetErrors()
            );
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/edit/{id}", Name = "usersedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetUserById.Query { Id = id });

        var allUserGroups = await Mediator.Send(new GetUserGroups.Query());
        var userGroups = allUserGroups.Result.Items.Select(
            p => new UsersEdit_ViewModel.UserGroupCheckboxItem_ViewModel
            {
                Id = p.Id,
                Label = p.Label,
                Selected = response.Result.UserGroups.Select(p => p.Id).Contains(p.Id),
            }
        );

        var model = new UsersEdit_ViewModel
        {
            Id = response.Result.Id,
            FirstName = response.Result.FirstName,
            LastName = response.Result.LastName,
            EmailAddress = response.Result.EmailAddress,
            IsActive = response.Result.IsActive,
            CurrentUserId = CurrentUser.UserId,
            IsAdmin = response.Result.IsAdmin,
            EmailAndPasswordEnabledForUsers = CurrentOrganization.EmailAndPasswordIsEnabledForUsers,
            UserGroups = userGroups.ToArray(),
        };
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/edit/{id}", Name = "usersedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsersEdit_ViewModel model, string id)
    {
        var input = new EditUser.Command
        {
            Id = id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailAddress = model.EmailAddress,
            UserGroups = model.UserGroups?.Where(p => p.Selected).Select(p => (ShortGuid)p.Id),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.FirstName} {model.LastName} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to update this admin. See the error below.",
                response.GetErrors()
            );
            var user = await Mediator.Send(new GetUserById.Query { Id = id });
            model.Id = id;
            model.EmailAndPasswordEnabledForUsers =
                CurrentOrganization.EmailAndPasswordIsEnabledForUsers;
            model.CurrentUserId = CurrentUser.UserId;
            model.IsAdmin = user.Result.IsAdmin;
            model.IsActive = user.Result.IsActive;

            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/reset-password/{id}", Name = "usersresetpassword")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToAction("Edit", new { id });
        }

        var response = await Mediator.Send(new GetUserById.Query { Id = id });

        if (response.Result.IsAdmin)
        {
            SetErrorMessage("You cannot reset an administrator's password from this screen.");
            return RedirectToAction("Edit", new { id });
        }

        var model = new UsersResetPassword_ViewModel
        {
            CurrentUserId = CurrentUser.UserId,
            Id = id,
            IsActive = response.Result.IsActive,
            IsAdmin = response.Result.IsAdmin,
            EmailAndPasswordEnabledForUsers = CurrentOrganization.EmailAndPasswordIsEnabledForUsers,
        };

        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/reset-password/{id}", Name = "usersresetpassword")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UsersResetPassword_ViewModel model, string id)
    {
        var input = new ResetPassword.Command
        {
            Id = id,
            ConfirmNewPassword = model.ConfirmNewPassword,
            NewPassword = model.NewPassword,
            SendEmail = model.SendEmail,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"Password was reset successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to reset this password. See the error below.",
                response.GetErrors()
            );
            var user = await Mediator.Send(new GetUserById.Query { Id = id });
            model.Id = id;
            model.EmailAndPasswordEnabledForUsers =
                CurrentOrganization.EmailAndPasswordIsEnabledForUsers;
            model.CurrentUserId = CurrentUser.UserId;
            model.IsAdmin = user.Result.IsAdmin;
            model.IsActive = user.Result.IsActive;

            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/suspend/{id}", Name = "userssuspend")]
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

    [Route(RAYTHA_ROUTE_PREFIX + "/users/restore/{id}", Name = "usersrestore")]
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

    [Route(RAYTHA_ROUTE_PREFIX + "/users/delete/{id}", Name = "userssdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await Mediator.Send(new DeleteUser.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"User has been deleted.");
        }
        else
        {
            SetErrorMessage(response.Error, response.GetErrors());
        }

        return RedirectToAction("Index");
    }

    public override async void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Users";
    }
}
