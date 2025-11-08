Here’s a tight Cursor prompt you can drop in.

---

**Task**
Migrate the legacy “Favorite / Unfavorite View” MVC endpoints to Razor Pages. Use a single action-style page named `ToggleFavorite` with page handlers. Update any `asp-route` usages to `asp-page` + `asp-page-handler`. We’re not keeping MVC RouteNames; Razor Pages links should be generated with `Url.Page` or tag helpers.

**Design**

* New page: `Areas/Admin/Pages/ContentTypes/Views/ToggleFavorite.cshtml` + `.cshtml.cs`
* Route: `/raytha/{contentTypeDeveloperName}/views/{viewId}/toggle-favorite/{handler?}`
* Handlers:

  * `OnPostFavoriteAsync()` sets favorite = true
  * `OnPostUnfavoriteAsync()` sets favorite = false
* Security/filters:

  * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
  * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]`
  * `[ValidateAntiForgeryToken]` for both posts
* Redirect after success/failure:

  * Redirect back to Content Items index for this view. If that’s already a Razor Page, use `RedirectToPage("Index", new { contentTypeDeveloperName = ..., viewId = ... })`. If not yet ported, keep pointing at the existing page you’ve used elsewhere (`contentitemsindex`) until it’s migrated.

**Create files**

`Areas/Admin/Pages/ContentTypes/Views/ToggleFavorite.cshtml`

```cshtml
@page "{contentTypeDeveloperName}/views/{viewId:guid}/toggle-favorite/{handler?}"
@model Areas.Admin.Pages.ContentTypes.Views.ToggleFavoriteModel
@{
    Layout = null; // action-style page, no UI
}
```

`Areas/Admin/Pages/ContentTypes/Views/ToggleFavorite.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Areas.Admin.Pages.ContentTypes.Views
{
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
    public class ToggleFavoriteModel : RaythaPageModel
    {
        private readonly IMediator _mediator;

        public ToggleFavoriteModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string ContentTypeDeveloperName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ViewId { get; set; }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostFavoriteAsync()
        {
            var response = await _mediator.Send(new ToggleViewAsFavoriteForAdmin.Command
            {
                UserId = CurrentUser.UserId!.Value,
                ViewId = CurrentView.Id,
                SetAsFavorite = true
            });

            if (response.Success)
                SetSuccessMessage("Successfully favorited view.");
            else
                SetErrorMessage(response.Error);

            return RedirectToContentItemsIndex();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUnfavoriteAsync()
        {
            var response = await _mediator.Send(new ToggleViewAsFavoriteForAdmin.Command
            {
                UserId = CurrentUser.UserId!.Value,
                ViewId = CurrentView.Id,
                SetAsFavorite = false
            });

            if (response.Success)
                SetSuccessMessage("Successfully unfavorited view.");
            else
                SetErrorMessage(response.Error);

            return RedirectToContentItemsIndex();
        }

        private IActionResult RedirectToContentItemsIndex()
        {
            // If Content Items index has been ported to Razor Pages:
            return RedirectToPage("Index", new
            {
                contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                viewId = CurrentView.Id
            });

            // If not yet ported, temporarily use the legacy named route (uncomment this block and remove the RedirectToPage above):
            // return RedirectToRoute("contentitemsindex", new {
            //     contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
            //     viewId = CurrentView.Id
            // });
        }
    }
}
```

**Update calling sites (examples)**

Favorite button (Razor Pages tag helpers):

```cshtml
<form method="post"
      asp-page="ToggleFavorite"
      asp-page-handler="Favorite"
      asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
      asp-route-viewId="@Model.CurrentView.Id">
  <button type="submit" class="btn btn-link btn-sm">Favorite</button>
</form>
```

Unfavorite button:

```cshtml
<form method="post"
      asp-page="ToggleFavorite"
      asp-page-handler="Unfavorite"
      asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
      asp-route-viewId="@Model.CurrentView.Id">
  <button type="submit" class="btn btn-link btn-sm text-danger">Unfavorite</button>
</form>
```

If you had any `asp-route="viewsfavorite"` or `asp-route="viewsunfavorite"` in `.cshtml`, replace with the forms above. For code that builds links programmatically, replace `Url.RouteUrl("viewsfavorite", ...)` with:

```csharp
Url.Page(
  pageName: "ToggleFavorite",
  handler: "Favorite",
  values: new { contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName, viewId = Model.CurrentView.Id }
)
```

…and similarly for Unfavorite.

**Notes**

* Using an action-style page keeps this consistent with other “do something then redirect” Razor Pages you already created (e.g., `ContentTypes/Fields/Delete`).
* Antiforgery is enforced by default with Razor Pages forms. If you submit these via AJAX later, include the token header as you did on Filter.
