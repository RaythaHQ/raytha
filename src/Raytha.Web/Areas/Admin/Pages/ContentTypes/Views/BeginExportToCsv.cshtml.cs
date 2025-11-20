using System.Collections.Generic;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.BackgroundTasks.Queries;
using Raytha.Application.ContentItems.Commands;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Admin.Pages.Shared;
using Raytha.Web.Areas.Admin.Pages.Shared.Models;
using Raytha.Web.Areas.Shared.Models;

namespace Raytha.Web.Areas.Admin.Pages.ContentTypes.Views;

[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
public class BeginExportToCsv : BaseHasFavoriteViewsPageModel
{
    [BindProperty]
    public bool ViewColumnsOnly { get; set; } = true;

    public string? JobId { get; set; }
    public string? PathBase { get; set; }
    public string BackToListUrl { get; set; } = string.Empty;

    public async Task<IActionResult> OnGet(
        string contentTypeDeveloperName,
        Guid viewId,
        bool json = false,
        string backToListUrl = ""
    )
    {
        if (json && !string.IsNullOrEmpty(Request.Query["jobId"]))
        {
            var jobId = Request.Query["jobId"].ToString();
            var response = await Mediator.Send(new GetBackgroundTaskById.Query { Id = jobId });
            return new JsonResult(response.Result);
        }

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = CurrentView.ContentType.LabelPlural,
                RouteName = RouteNames.ContentItems.Index,
                RouteValues = new Dictionary<string, string>
                {
                    { "contentTypeDeveloperName", CurrentView.ContentType.DeveloperName },
                },
                IsActive = false,
                Icon = SidebarIcons.ContentItems,
            },
            new BreadcrumbNode
            {
                Label = "Views",
                RouteName = RouteNames.ContentTypes.Views.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Export to CSV",
                RouteName = RouteNames.ContentTypes.Views.BeginExportToCsv,
                IsActive = true,
            }
        );

        PathBase = CurrentOrganization.PathBase;
        BackToListUrl = backToListUrl;
        return Page();
    }

    public async Task<IActionResult> OnPost(
        string contentTypeDeveloperName,
        Guid viewId,
        string backToListUrl = ""
    )
    {
        var input = new BeginExportContentItemsToCsv.Command
        {
            ViewId = CurrentView.Id,
            ExportOnlyColumnsFromView = ViewColumnsOnly,
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Export in progress.");
            JobId = response.Result;
            PathBase = CurrentOrganization.PathBase;
            BackToListUrl = backToListUrl;
            return Page();
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to begin this export. See the error below.",
                response.GetErrors()
            );
            BackToListUrl = backToListUrl;
            return Page();
        }
    }
}
