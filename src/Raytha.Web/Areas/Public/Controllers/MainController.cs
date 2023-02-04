using System;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.Views.Queries;
using Raytha.Domain.Entities;
using Raytha.Web.Areas.Public.DbViewEngine;

namespace Raytha.Web.Areas.Public.Controllers;

[Area("Public")]
public class MainController : BaseController
{
    [Route($"", Name = "emptyroute")]
    [Route("{*route}", Name = "defaultroute")]
    public async Task<IActionResult> Index(string route, 
                                           string search = "",
                                           string filter = "",
                                           string orderBy = "",
                                           int pageNumber = 1,
                                           int pageSize = 25)
    {
        try
        {
            pageSize = pageSize > 1000 ? 1000 : pageSize;
            if (string.IsNullOrEmpty(route) || route == "/")
            {
                var input = new GetContentItemById.Query { Id = CurrentOrganization.HomePageId.Value };
                var response = await Mediator.Send(input);
                if (!response.Result.IsPublished)
                {
                    return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel());
                }
                var model = ContentItem_RenderModel.GetProjection(response.Result);
                var contentType = ContentType_RenderModel.GetProjection(response.Result.ContentType);
                return new ContentItemActionViewResult(response.Result.WebTemplate.DeveloperName, model, contentType);
            }
            else
            {
                var input = new GetRouteByPath.Query { Path = route.TrimEnd('/') };
                var response = await Mediator.Send(input);

                if (response.Result.PathType == "ContentItem")
                {
                    var contentItem = await Mediator.Send(new GetContentItemById.Query { Id = response.Result.ContentItemId.Value });
                    if (!contentItem.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel());
                    }
                    var model = ContentItem_RenderModel.GetProjection(contentItem.Result);
                    var contentType = ContentType_RenderModel.GetProjection(contentItem.Result.ContentType);
                    return new ContentItemActionViewResult(contentItem.Result.WebTemplate.DeveloperName, model, contentType);
                }
                else if (response.Result.PathType == "View")
                {
                    var view = await Mediator.Send(new GetViewById.Query { Id = response.Result.ViewId.Value });
                    if (!view.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel());
                    }
                    var contentItems = await Mediator.Send(new GetContentItems.Query
                    {
                        ViewId = response.Result.ViewId.Value,
                        Search = search,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        OrderBy = orderBy,
                        Filter = filter
                    });
                    var modelAsList = ContentItemListResult_RenderModel.GetProjection(contentItems.Result, view.Result, search, filter, orderBy, pageSize, pageNumber);
                    var contentType = ContentType_RenderModel.GetProjection(view.Result.ContentType);
                    
                    return new ContentItemActionViewResult(
                        view.Result.WebTemplate.DeveloperName,
                        modelAsList,
                        contentType);
                }
                throw new Exception("Unknown content type");
            }
        }
        catch (NotFoundException)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, errorModel);
        }
        catch (UnauthorizedAccessException)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, errorModel);
        }
        catch (Exception)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error500, 500, errorModel);
        }
    }

    [Route($"raytha/500", Name = "errorroute")]
    public IActionResult Error()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error500, 500, errorModel);
    }

    [Route("raytha/403", Name = "forbidden")]
    public IActionResult Forbidden()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, errorModel);
    }

    [Route("raytha/404", Name = "notfound")]
    public IActionResult EntityNotFound()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 403, errorModel);
    }
}