using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Common.Utils;
using Raytha.Application.RaythaFunctions.Commands;
using Raytha.Application.RaythaFunctions.Queries;
using Raytha.Domain.Entities;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.RaythaFunctions;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Filters;
using Raytha.Web.Utils;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(ForbidAccessIfRaythaFunctionsAreDisabledFilterAttribute))]
[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class RaythaFunctionsController : BaseController
{
    [ServiceFilter(typeof(SetPaginationInformationFilterAttribute))]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions", Name = "functionsindex")]
    public async Task<IActionResult> Index(string search = "", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetRaythaFunctions.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(rf => new RaythaFunctionsListItem_ViewModel
        {
            Id = rf.Id,
            Name = rf.Name,
            DeveloperName = rf.DeveloperName,
            TriggerType = rf.TriggerType.Label,
            IsActive = rf.IsActive.YesOrNo(),
            LastModificationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(rf.LastModificationTime),
            LastModifierUser = rf.LastModifierUser?.FullName ?? "N/A",

        });

        var viewModel = new List_ViewModel<RaythaFunctionsListItem_ViewModel>(items, response.Result.TotalCount);

        return View(viewModel);
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/create", Name = "functionscreate")]
    public IActionResult Create()
    {
        return View(new RaythaFunctionsCreate_ViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/create", Name = "functionscreate")]
    public async Task<IActionResult> Create(RaythaFunctionsCreate_ViewModel model)
    {
        var input = new CreateRaythaFunction.Command
        {
            Name = model.Name,
            DeveloperName = model.DeveloperName,
            TriggerType = model.TriggerType,
            IsActive = model.IsActive,
            Code = model.Code,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Name} was created successfully.");

            return RedirectToAction(nameof(Index));
        }
        else
        {
            SetErrorMessage("There was an error attempting to create this function. See the error below.", response.GetErrors());

            return View(model);
        }
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/edit/{{id}}", Name = "functionsedit")]
    public async Task<IActionResult> Edit(string id)
    {
        var response = await Mediator.Send(new GetRaythaFunctionById.Query { Id = id });

        var model = new RaythaFunctionsEdit_ViewModel
        {
            Id = id,
            Name = response.Result.Name,
            DeveloperName = response.Result.DeveloperName,
            TriggerType = response.Result.TriggerType,
            IsActive = response.Result.IsActive,
            Code = response.Result.Code,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/edit/{{id}}", Name = "functionsedit")]
    public async Task<IActionResult> Edit(string id, RaythaFunctionsEdit_ViewModel model)
    {
        var input = new EditRaythaFunction.Command
        {
            Id = id,
            Name = model.Name,
            TriggerType = model.TriggerType,
            IsActive = model.IsActive,
            Code = model.Code,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{model.Name} was updated successfully.");

            return RedirectToAction(nameof(Edit), new { id });
        }
        else
        {
            SetErrorMessage("There was an error attempting to update this function. See the error below.", response.GetErrors());

            model.Id = id;

            return View(model);
        }
    }

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/delete/{{id}}", Name = "functionsdelete")]
    public async Task<IActionResult> Delete(string id)
    {
        var input = new DeleteRaythaFunction.Command { Id = id };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Function has been deleted.");

            return RedirectToAction(nameof(Index));
        }
        else
        {
            SetErrorMessage("There was an error deleting this function", response.GetErrors());

            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/edit/{{id}}/revisions", Name = "functionsrevisionsindex")]
    public async Task<IActionResult> Revisions(string id, string orderBy = $"CreationTime {SortOrder.DESCENDING}", int pageNumber = 1, int pageSize = 50)
    {
        var input = new GetRaythaFunctionRevisionsByRaythaFunctionId.Query
        {
            Id = id,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy,
        };

        var response = await Mediator.Send(input);

        var items = response.Result.Items.Select(rfr => new RaythaFunctionsRevisionsListItem_ViewModel
        {
            Id = rfr.Id,
            Code = rfr.Code,
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(rfr.CreationTime),
            CreatorUser = rfr.CreatorUser?.FullName ?? "N/A",
        });

        var viewModel = new RaythaFunctionsRevisionsPagination_ViewModel(items, response.Result.TotalCount)
        {
            FunctionId = id,
        };

        return View(viewModel);
    }

    [HttpPost]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/edit/{{id}}/revisions/{{revisionId}}", Name = "functionsrevisionsrevert")]
    public async Task<IActionResult> RevisionRevert(string id, string revisionId)
    {
        var input = new RevertRaythaFunction.Command { Id = revisionId };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Function has been reverted.");

            return RedirectToAction(nameof(Edit), new { id });
        }
        else
        {
            SetErrorMessage("There was an error reverting this function", response.GetErrors());

            return RedirectToAction(nameof(Revisions), new { id });
        }
    }

    [AllowAnonymous]
    [Route($"{RAYTHA_ROUTE_PREFIX}/functions/execute/{{{RouteConstants.FUNCTION_DEVELOPER_NAME}}}", Name = "functionexecute")]
    public async Task<IActionResult> Execute(string functionDeveloperName)
    {
        var input = new ExecuteRaythaFunction.Command
        {
            DeveloperName = functionDeveloperName,
            RequestMethod = HttpContext.Request.Method,
            QueryJson = JsonConvert.SerializeObject(HttpContext.Request.Query),
            PayloadJson = JsonConvert.SerializeObject(HttpContext.Request.HasFormContentType ? HttpContext.Request.Form : null),
        };

        var response = await Mediator.Send(input);
        if (response.Success)
        {
            dynamic result = response.Result;
            return result.contentType switch
            {
                "application/json" => Json(result.body),
                "text/html" => Content(result.body, result.contentType),
                "redirectToUrl" => Redirect(result.body),
                "statusCode" => StatusCode(result.statusCode, result.body),
                _ => throw new NotSupportedException()
            };
        }
        else
        {
            var statusCode = response.GetErrors().First().PropertyName switch
            {
                "IsActive" => StatusCodes.Status404NotFound,
                "Queue of functions" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            };

            return StatusCode(statusCode, response.Error);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Functions";
    }
}