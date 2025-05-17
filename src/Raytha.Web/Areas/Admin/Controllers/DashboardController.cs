using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Raytha.Application.Common.Security;
using Raytha.Application.ContentItems.Commands;
using Raytha.Application.Dashboard.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Views.Dashboard;

namespace Raytha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = RaythaClaimTypes.IsAdmin)]
public class DashboardController : BaseController
{
    private const decimal ONE_GIGABYTE = 1000000000;
    private const decimal ONE_MEGABYTE = 1000000;

    [Route(RAYTHA_ROUTE_PREFIX, Name = "dashboardindex")]
    public async Task<IActionResult> Index()
    {
        var response = await Mediator.Send(new GetDashboardMetrics.Query());
        var viewModel = new Dashboard_ViewModel
        {
            CurrentFileStorageSizeInGb = Math.Round(
                response.Result.FileStorageSize / ONE_GIGABYTE,
                2
            ),
            CurrentDbSizeInMb = Math.Round(response.Result.DbSize, 2),
            CurrentTotalContentItems = response.Result.TotalContentItems,
            CurrentTotalUsers = response.Result.TotalUsers,
            FileStorageProvider = FileStorageProviderSettings.FileStorageProvider,
            AllowedMimeTypes = FileStorageProviderSettings.AllowedMimeTypes,
            UseDirectUploadToCloud = FileStorageProviderSettings.UseDirectUploadToCloud,
            MaxTotalDiskSpaceInGb = Math.Round(
                FileStorageProviderSettings.MaxTotalDiskSpace / ONE_GIGABYTE,
                2
            ),
            MaxTotalDbSizeInMb = Math.Round(
                FileStorageProviderSettings.MaxTotalDbSize / ONE_MEGABYTE,
                2
            ),
        };
        return View(viewModel);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);
        ViewData["ActiveMenu"] = "Dashboard";
    }
}
