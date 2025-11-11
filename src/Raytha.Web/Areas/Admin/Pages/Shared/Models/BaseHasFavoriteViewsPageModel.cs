using Microsoft.AspNetCore.Mvc.Filters;
using Raytha.Application.Views.Queries;

namespace Raytha.Web.Areas.Admin.Pages.Shared.Models;

public abstract class BaseHasFavoriteViewsPageModel : BaseContentTypeContextPageModel
{
    public List<FavoriteViewsListItemViewModel> FavoriteViews { get; set; } = new();

    public bool HasFavorites
    {
        get { return FavoriteViews != null && FavoriteViews.Any(); }
    }

    public bool CurrentViewIsFavorite
    {
        get { return HasFavorites && FavoriteViews.Any(p => p.Id == CurrentView.Id); }
    }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next
    )
    {
        var currentView = await GetCurrentView(context);
        var favoriteViews = await Mediator.Send(
            new GetFavoriteViewsForAdmin.Query
            {
                ContentTypeId = currentView.ContentTypeId,
                UserId = CurrentUser.UserId!.Value,
            }
        );

        FavoriteViews = favoriteViews
            .Result.Items.Select(p => new FavoriteViewsListItemViewModel
            {
                Id = p.Id,
                Label = p.Label,
            })
            .ToList();

        await base.OnPageHandlerExecutionAsync(context, next);
    }

    public record FavoriteViewsListItemViewModel
    {
        public required string Id { get; set; }
        public required string Label { get; set; }
    }
}
