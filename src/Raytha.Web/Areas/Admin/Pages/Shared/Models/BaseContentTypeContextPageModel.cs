using CSharpVitamins;
using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Admins.Commands;
using Raytha.Application.Admins.Queries;
using Raytha.Application.Common.Exceptions;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views;
using Raytha.Application.Views.Queries;
using Raytha.Domain.ValueObjects;
using Raytha.Web.Utils;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

public abstract class BaseContentTypeContextPageModel : BaseAdminPageModel
{
    public ViewDto CurrentView { get; set; }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next
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
        var contentType = await Mediator.Send(
            new GetContentTypeByDeveloperName.Query { DeveloperName = contentTypeDeveloperName }
        );

        if (viewId == ShortGuid.Empty)
        {
            var loggedInUser = await Mediator.Send(
                new GetAdminById.Query { Id = CurrentUser.UserId.Value }
            );
            var lastViewed = loggedInUser.Result.RecentlyAccessedViews.FirstOrDefault(p =>
                p.ContentTypeId == contentType.Result.Id
            );

            if (lastViewed != null)
            {
                try
                {
                    var response = await Mediator.Send(
                        new GetViewById.Query { Id = lastViewed.ViewId }
                    );
                    viewDto = response.Result;
                }
                catch (NotFoundException ex)
                {
                    var response = await Mediator.Send(
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
                var response = await Mediator.Send(
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
            var response = await Mediator.Send(new GetViewById.Query { Id = viewId.Value });

            if (response.Result.ContentType.DeveloperName != contentType.Result.DeveloperName)
            {
                throw new UnauthorizedAccessException(
                    "Content type in route path does not match content type for the view."
                );
            }

            await Mediator.Send(
                new UpdateRecentlyAccessedView.Command
                {
                    ViewId = viewId.Value,
                    ContentTypeId = contentType.Result.Id,
                    UserId = CurrentUser.UserId.Value,
                }
            );
            viewDto = response.Result;
        }

        CurrentView = viewDto;
        await base.OnPageHandlerExecutionAsync(context, next);
    }
}
