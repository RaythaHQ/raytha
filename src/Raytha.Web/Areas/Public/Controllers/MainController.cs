using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Models.RenderModels;
using Raytha.Application.ContentItems;
using Raytha.Application.ContentItems.Queries;
using Raytha.Application.ContentTypes;
using Raytha.Application.Routes.Queries;
using Raytha.Application.Themes.Queries;
using Raytha.Application.Themes.WebTemplates.Queries;
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
                                           int pageSize = 0)
    {
        try
        {
            if (string.IsNullOrEmpty(route) || route == "/")
            {
                if (CurrentOrganization.HomePageType == Route.CONTENT_ITEM_TYPE)
                {
                    var input = new GetContentItemById.Query { Id = CurrentOrganization.HomePageId.Value };
                    var response = await Mediator.Send(input);
                    if (!response.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel(), ViewData);
                    }

                    var webTemplateResponse = await Mediator.Send(new GetWebTemplateByContentItemId.Query
                    {
                        ContentItemId = CurrentOrganization.HomePageId.Value,
                        ThemeId = CurrentOrganization.ActiveThemeId,
                    });

                    var model = ContentItem_RenderModel.GetProjection(response.Result, webTemplateResponse.Result.DeveloperName);
                    var contentType = ContentType_RenderModel.GetProjection(response.Result.ContentType);

                    return new ContentItemActionViewResult(webTemplateResponse.Result, model, contentType, ViewData);
                }
                else if (CurrentOrganization.HomePageType == Route.VIEW_TYPE)
                {
                    var view = await Mediator.Send(new GetViewById.Query { Id = CurrentOrganization.HomePageId.Value });
                    if (!view.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel(), ViewData);
                    }

                    pageSize = pageSize <= 0 ? view.Result.DefaultNumberOfItemsPerPage : pageSize > view.Result.MaxNumberOfItemsPerPage ? view.Result.MaxNumberOfItemsPerPage : pageSize;

                    var contentItems = await Mediator.Send(new GetContentItems.Query
                    {
                        ViewId = view.Result.Id,
                        Search = search,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        OrderBy = orderBy,
                        Filter = filter
                    });

                    var webTemplateContentItemRelationsResponse = await Mediator.Send(new GetWebTemplateContentItemRelationsByContentTypeId.Query
                    {
                        ThemeId = CurrentOrganization.ActiveThemeId,
                        ContentTypeId = view.Result.ContentTypeId,
                    });

                    var webTemplateDeveloperNamesByContentItemId = webTemplateContentItemRelationsResponse.Result.ToDictionary(wtr => wtr.ContentItemId, wtr => wtr.WebTemplate.DeveloperName);
                    
                    var modelAsList = ContentItemListResult_RenderModel.GetProjection(contentItems.Result, webTemplateDeveloperNamesByContentItemId, view.Result, search, filter, orderBy, pageSize, pageNumber);
                    var contentType = ContentType_RenderModel.GetProjection(view.Result.ContentType);

                    var webTemplateResponse = await Mediator.Send(new GetWebTemplateByViewId.Query
                    {
                        ThemeId = CurrentOrganization.ActiveThemeId,
                        ViewId = view.Result.Id,
                    });

                    return new ContentItemActionViewResult(
                        webTemplateResponse.Result,
                        modelAsList,
                        contentType,
                        ViewData);
                }
                throw new Exception("Unknown content type");
            }
            else
            {
                var input = new GetRouteByPath.Query { Path = route.TrimEnd('/') };
                var response = await Mediator.Send(input);

                if (response.Result.PathType == Route.CONTENT_ITEM_TYPE)
                {
                    var contentItem = await Mediator.Send(new GetContentItemById.Query { Id = response.Result.ContentItemId.Value });
                    if (!contentItem.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel(), ViewData);
                    }

                    var webTemplateResponse = await Mediator.Send(new GetWebTemplateByContentItemId.Query
                    {
                        ThemeId = CurrentOrganization.ActiveThemeId,
                        ContentItemId = contentItem.Result.Id,
                    });

                    var model = ContentItem_RenderModel.GetProjection(contentItem.Result, webTemplateResponse.Result.DeveloperName);
                    var contentType = ContentType_RenderModel.GetProjection(contentItem.Result.ContentType);

                    return new ContentItemActionViewResult(webTemplateResponse.Result, model, contentType, ViewData);
                }
                else if (response.Result.PathType == Route.VIEW_TYPE)
                {
                    var view = await Mediator.Send(new GetViewById.Query { Id = response.Result.ViewId.Value });
                    if (!view.Result.IsPublished)
                    {
                        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, new GenericError_RenderModel(), ViewData);
                    }

                    pageSize = pageSize <= 0 ? view.Result.DefaultNumberOfItemsPerPage : pageSize > view.Result.MaxNumberOfItemsPerPage ? view.Result.MaxNumberOfItemsPerPage : pageSize;

                    var contentItems = await Mediator.Send(new GetContentItems.Query
                    {
                        ViewId = response.Result.ViewId.Value,
                        Search = search,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        OrderBy = orderBy,
                        Filter = filter
                    });

                    var webTemplateContentItemRelationsResponse = await Mediator.Send(new GetWebTemplateContentItemRelationsByContentTypeId.Query
                    {
                        ThemeId = CurrentOrganization.ActiveThemeId,
                        ContentTypeId = view.Result.ContentTypeId,
                    });

                    var webTemplateDeveloperNamesByContentItemId = webTemplateContentItemRelationsResponse.Result.ToDictionary(wtr => wtr.ContentItemId, wtr => wtr.WebTemplate.DeveloperName);

                    var modelAsList = ContentItemListResult_RenderModel.GetProjection(contentItems.Result, webTemplateDeveloperNamesByContentItemId, view.Result, search, filter, orderBy, pageSize, pageNumber);
                    var contentType = ContentType_RenderModel.GetProjection(view.Result.ContentType);

                    var webTemplateResponse = await Mediator.Send(new GetWebTemplateByViewId.Query
                    {
                        ThemeId = CurrentOrganization.ActiveThemeId,
                        ViewId = view.Result.Id,
                    });

                    return new ContentItemActionViewResult(
                        webTemplateResponse.Result,
                        modelAsList,
                        contentType, 
                        ViewData);
                }
                throw new Exception("Unknown content type");
            }
        }
        catch (NotFoundException)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 404, errorModel, ViewData);
        }
        catch (UnauthorizedAccessException)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, errorModel, ViewData);
        }
        catch (Exception)
        {
            var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
            return new ErrorActionViewResult(BuiltInWebTemplate.Error500, 500, errorModel, ViewData);
        }
    }

    [Route($"raytha/500", Name = "errorroute")]
    public IActionResult Error()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error500, 500, errorModel, ViewData);
    }

    [Route("raytha/403", Name = "forbidden")]
    public IActionResult Forbidden()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error403, 403, errorModel, ViewData);
    }

    [Route("raytha/404", Name = "notfound")]
    public IActionResult EntityNotFound()
    {
        var errorModel = new GenericError_RenderModel { ErrorId = ShortGuid.NewGuid() };
        return new ErrorActionViewResult(BuiltInWebTemplate.Error404, 403, errorModel, ViewData);
    }
}