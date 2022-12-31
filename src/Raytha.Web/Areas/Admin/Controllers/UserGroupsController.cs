using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.UserGroups.Commands;
using Raytha.Application.UserGroups.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Roles;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Areas.Admin.Views.UserGroups;
using Raytha.Web.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class UserGroupsController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups", Name = "usergroupsindex")]
    public async Task<IActionResult> Index(string search = "", string orderBy = $"Label {SortOrder.ASCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetUserGroups.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy
        };

        var response = await Mediator.Send(input);
        var items = response.Result.Items.Select(p => new UserGroupsListItem_ViewModel
        {
            DeveloperName = p.DeveloperName,
            Label = p.Label,
            Id = p.Id,
        });

        var viewModel = new List_ViewModel<UserGroupsListItem_ViewModel>(items, response.Result.TotalCount);
        return View(viewModel);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups/create", Name = "usergroupscreate")]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserGroup_ViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups/create", Name = "usergroupscreate")]
    public async Task<IActionResult> Create(CreateUserGroup_ViewModel model)
    {
        var input = new CreateUserGroup.Command
        {
            Label = model.Label,
            DeveloperName = model.DeveloperName
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was created successfully.");
            return RedirectToAction("Index");
        }
        else
        {
            SetErrorMessage("There was an error attempting to create this user group. See the error below.", response.GetErrors());
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups/edit/{id}", Name = "usergroupsedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetUserGroupById.Query { Id = id });

        var model = new EditUserGroup_ViewModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName
        };
        return View(model);
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups/edit/{id}", Name = "usergroupsedit")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditRole_ViewModel model, string id)
    {
        var input = new EditUserGroup.Command
        {
            Id = id,
            Label = model.Label
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Label} was updated successfully.");
            return RedirectToAction("Edit", new { id });
        }
        {
            SetErrorMessage("There was an error attempting to update this user group. See the error below.", response.GetErrors());
            model.Id = id;
            return View(model);
        }
    }

    [Route(RAYTHA_ROUTE_PREFIX + "/users/groups/delete/{id}", Name = "usergroupsdelete")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var response = await Mediator.Send(new DeleteUserGroup.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"User group has been deleted.");
        }
        else
        {
            SetErrorMessage("There was a problem deleting this user group", response.GetErrors());
        }

        return RedirectToAction("Index");
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Users";
    }
}
