using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using CSharpVitamins;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.Common.Interfaces;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Queries;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Areas.Admin.Views.Shared.ViewModels;
using Raytha.Web.Utils;

namespace Raytha.Web.Filters;

public class GetOrSetRecentlyAccessedViewFilterAttribute : ActionFilterAttribute
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentOrganization _currentOrganization;

    public GetOrSetRecentlyAccessedViewFilterAttribute(
        IMediator mediator,
        ICurrentUser currentUser,
        ICurrentOrganization currentOrganization
    )
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _currentOrganization = currentOrganization;
    }

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        string contentTypeDeveloperName =
            context.RouteData.Values[RouteConstants.CONTENT_TYPE_DEVELOPER_NAME] as string;
        ShortGuid viewId = null;
        if (context.RouteData.Values.ContainsKey("viewId"))
        {
            ShortGuid.TryParse(context.RouteData.Values["viewId"].ToString(), out viewId);
        }

        ViewDto viewDto;
        var contentType = await _mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );

        if (viewId == ShortGuid.Empty)
        {
            var loggedInUser = await _mediator.Send(
                new GetAdminById.Query { Id = _currentUser.UserId.Value }
            );
            var lastViewed = loggedInUser.Result.RecentlyAccessedViews.FirstOrDefault(p =>
                p.ContentTypeId == contentType.Result.Id
            );

            if (lastViewed != null)
            {
                try
                {
                    var response = await _mediator.Send(
                        new GetViewById.Query { Id = lastViewed.ViewId }
                    );
                    viewDto = response.Result;
                }
                catch (NotFoundException ex)
                {
                    var response = await _mediator.Send(
                        new GetViews.Query
                        {
                            ContentTypeId = contentType.Result.Id,
                            PageSize = 1,
                            OrderBy = $"CreationTime {SortOrder.ASCENDING}",
                        }
                    );
                    viewDto = response.Result.Items.First();
                }
            }
            else
            {
                var response = await _mediator.Send(
                    new GetViews.Query
                    {
                        ContentTypeId = contentType.Result.Id,
                        PageSize = 1,
                        OrderBy = $"CreationTime {SortOrder.ASCENDING}",
                    }
                );
                viewDto = response.Result.Items.First();
            }
            viewId = viewDto.Id;
        }
        else
        {
            var response = await _mediator.Send(new GetViewById.Query { Id = viewId.Value });

            if (response.Result.ContentType.DeveloperName != contentType.Result.DeveloperName)
            {
                throw new UnauthorizedAccessException(
                    "Content type in route path does not match content type for the view."
                );
            }

            await _mediator.Send(
                new UpdateRecentlyAccessedView.Command
                {
                    ViewId = viewId.Value,
                    ContentTypeId = contentType.Result.Id,
                    UserId = _currentUser.UserId.Value,
                }
            );
            viewDto = response.Result;
        }

        if (context.Controller is Controller controller)
        {
            context.HttpContext.Items["CurrentView"] = viewDto;
            context.HttpContext.Items["ViewId"] = viewId;

            await next();

            if (
                controller.ViewData.Model
                is IMustHaveCurrentViewForList paginationModelForCurrentView
            )
            {
                var currentViewModel = new CurrentViewForList_ViewModel
                {
                    Id = viewDto.Id,
                    Label = viewDto.Label,
                    Columns = viewDto.Columns,
                    Filter = viewDto.Filter,
                    ContentTypeId = viewDto.ContentTypeId,
                    ContentTypeLabelSingular = viewDto.ContentType.LabelSingular,
                    ContentTypeLabelPlural = viewDto.ContentType.LabelPlural,
                    ContentTypeDescription = viewDto.ContentType.Description,
                    ContentTypeDeveloperName = viewDto.ContentType.DeveloperName,
                    IsPublished = viewDto.IsPublished,
                    RoutePath = viewDto.RoutePath,
                    IsHomePage = _currentOrganization.HomePageId == viewDto.Id,
                };

                paginationModelForCurrentView.CurrentView = currentViewModel;
                controller.ViewData.Model = paginationModelForCurrentView;
            }

            if (
                controller.ViewData.Model
                is IMustHaveFavoriteViewsForList paginationModelForFavorites
            )
            {
                var favoriteViews = await _mediator.Send(
                    new GetFavoriteViewsForAdmin.Query
                    {
                        ContentTypeId = viewDto.ContentTypeId,
                        UserId = _currentUser.UserId.Value,
                    }
                );

                var favoriteViewsViewModel =
                    favoriteViews.Result.TotalCount > 0
                        ? favoriteViews.Result.Items.Select(p => new FavoriteViewsForList_ViewModel
                        {
                            Id = p.Id,
                            Label = p.Label,
                        })
                        : new List<FavoriteViewsForList_ViewModel>();

                paginationModelForFavorites.FavoriteViews = favoriteViewsViewModel;
                controller.ViewData.Model = paginationModelForFavorites;
            }

            controller.ViewData["ActiveMenu"] = viewDto.ContentType.DeveloperName;
        }
        else
        {
            await next();
        }
    }
}
