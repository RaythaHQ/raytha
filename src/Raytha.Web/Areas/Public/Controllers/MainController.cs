using System;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.SitePages.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Public.DbViewEngine;
using Raytha.Web.Authentication;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
public class MainController : BaseController
{
    private IAuthorizationService _authorizationService;
    protected IAuthorizationService AuthorizationService =>
        _authorizationService ??= HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

    private async Task<IActionResult> HandleHomePageAsync(
        string search,
        string filter,
        string orderBy,
        int pageNumber,
        int pageSize
    )
    {
        return CurrentOrganization.HomePageType switch
        {
            Domain.Entities.Route.CONTENT_ITEM_TYPE =>
                await RenderContentItemAsync(CurrentOrganization.HomePageId.Value),
            Domain.Entities.Route.VIEW_TYPE =>
                await RenderViewAsync(
                    CurrentOrganization.HomePageId.Value,
                    search,
                    filter,
                    orderBy,
                    pageNumber,
                    pageSize
                ),
            Domain.Entities.Route.SITE_PAGE_TYPE =>
                await RenderSitePageAsync(CurrentOrganization.HomePageId.Value),
            _ => throw new Exception("Unknown content type"),
        };
    }

    private async Task<IActionResult> HandleRouteAsync(
        string route,
        string search,
        string filter,
        string orderBy,
        int pageNumber,
        int pageSize
    )
    {
        var input = new GetRouteByPath.Query { Path = route.TrimEnd('/') };
        var response = await Mediator.Send(input);

        return response.Result.PathType switch
        {
            Domain.Entities.Route.CONTENT_ITEM_TYPE =>
                await RenderContentItemAsync(response.Result.ContentItemId.Value),
            Domain.Entities.Route.VIEW_TYPE =>
                await RenderViewAsync(
                    response.Result.ViewId.Value,
                    search,
                    filter,
                    orderBy,
                    pageNumber,
                    pageSize
                ),
            Domain.Entities.Route.SITE_PAGE_TYPE =>
                await RenderSitePageAsync(response.Result.SitePageId.Value),
            _ => throw new Exception("Unknown content type"),
        };
    }

    private async Task<IActionResult> RenderContentItemAsync(ShortGuid contentItemId)
    {
        var response = await Mediator.Send(new GetContentItemById.Query { Id = contentItemId });
        var previewDraft = Request.Query["previewDraft"] == "true"
            && await CanPreviewContentItemDraftsAsync(response.Result.ContentType.DeveloperName);

        if (!response.Result.IsPublished && !previewDraft)
        {
            return BuildNotFoundResult();
        }

        var webTemplateResponse = await Mediator.Send(
            new GetWebTemplateByContentItemId.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentItemId = response.Result.Id,
            }
        );

        var model = ContentItem_RenderModel.GetProjection(
            response.Result,
            webTemplateResponse.Result.DeveloperName,
            previewDraft
        );
        var contentType = ContentType_RenderModel.GetProjection(response.Result.ContentType);

        return new ContentItemActionViewResult(
            webTemplateResponse.Result,
            model,
            contentType,
            ViewData
        );
    }

    private async Task<IActionResult> RenderViewAsync(
        ShortGuid viewId,
        string search,
        string filter,
        string orderBy,
        int pageNumber,
        int pageSize
    )
    {
        var view = await Mediator.Send(new GetViewById.Query { Id = viewId });
        if (!view.Result.IsPublished)
        {
            return BuildNotFoundResult();
        }

        var normalizedPageSize = NormalizePageSize(view.Result, pageSize);

        var contentItems = await Mediator.Send(
            new GetContentItems.Query
            {
                ViewId = view.Result.Id,
                Search = search,
                PageNumber = pageNumber,
                PageSize = normalizedPageSize,
                OrderBy = orderBy,
                Filter = filter,
            }
        );

        var webTemplateContentItemRelationsResponse = await Mediator.Send(
            new GetWebTemplateContentItemRelationsByContentTypeId.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = view.Result.ContentTypeId,
            }
        );

        var webTemplateDeveloperNamesByContentItemId =
            webTemplateContentItemRelationsResponse.Result.ToDictionary(
                wtr => wtr.ContentItemId,
                wtr => wtr.WebTemplate.DeveloperName
            );

        var modelAsList = ContentItemListResult_RenderModel.GetProjection(
            contentItems.Result,
            webTemplateDeveloperNamesByContentItemId,
            view.Result,
            search,
            filter,
            orderBy,
            normalizedPageSize,
            pageNumber
        );
        var contentType = ContentType_RenderModel.GetProjection(view.Result.ContentType);

        var webTemplateResponse = await Mediator.Send(
            new GetWebTemplateByViewId.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ViewId = view.Result.Id,
            }
        );

        return new ContentItemActionViewResult(
            webTemplateResponse.Result,
            modelAsList,
            contentType,
            ViewData
        );
    }

    private async Task<IActionResult> RenderSitePageAsync(ShortGuid sitePageId)
    {
        var sitePage = await Mediator.Send(new GetSitePageById.Query { Id = sitePageId });

        var previewDraft = Request.Query["previewDraft"] == "true"
            && await CanPreviewSitePageDraftsAsync();

        if (!sitePage.Result.IsPublished && !previewDraft)
        {
            return BuildNotFoundResult();
        }

        var webTemplateResponse = await Mediator.Send(
            new GetWebTemplateById.Query { Id = sitePage.Result.WebTemplateId }
        );

        var model = SitePage_RenderModel.GetProjection(
            sitePage.Result,
            webTemplateResponse.Result.DeveloperName
        );

        return new SitePageActionViewResult(
            webTemplateResponse.Result,
            model,
            sitePage.Result,
            ViewData,
            previewDraft
        );
    }

    private static int NormalizePageSize(ViewDto view, int requestedPageSize)
    {
        if (requestedPageSize <= 0)
        {
            return view.DefaultNumberOfItemsPerPage;
        }

        return requestedPageSize > view.MaxNumberOfItemsPerPage
            ? view.MaxNumberOfItemsPerPage
            : requestedPageSize;
    }

    private IActionResult BuildNotFoundResult()
    {
        return new ErrorActionViewResult(
            BuiltInWebTemplate.Error404,
            404,
            new GenericError_RenderModel(),
            ViewData
        );
    }

    private async Task<bool> CanPreviewSitePageDraftsAsync()
    {
        var result = await AuthorizationService.AuthorizeAsync(
            User,
            BuiltInSystemPermission.MANAGE_SITE_PAGES_PERMISSION
        );
        return result.Succeeded;
    }

    private async Task<bool> CanPreviewContentItemDraftsAsync(string contentTypeDeveloperName)
    {
        var result = await AuthorizationService.AuthorizeAsync(
            User,
            contentTypeDeveloperName,
            ContentItemOperations.Edit
        );
        return result.Succeeded;
    }

        [Route($"", Name = "emptyroute")]
    [Route("{*route}", Name = "defaultroute")]
    public async Task<IActionResult> Index(
        string route,
        string search = "",
        string filter = "",
        string orderBy = "",
        int pageNumber = 1,
        int pageSize = 0
    )
    {
        try
        {
            if (string.IsNullOrEmpty(route) || route == "/")
            {
                return await HandleHomePageAsync(search, filter, orderBy, pageNumber, pageSize);
            }

            return await HandleRouteAsync(route, search, filter, orderBy, pageNumber, pageSize);
        }
        catch (NotFoundException)
        {
            Logger.LogInformation("Not found exception: {route}", route);
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(
                BuiltInWebTemplate.Error404,
                404,
                errorModel,
                ViewData
            );
        }
    }
}
