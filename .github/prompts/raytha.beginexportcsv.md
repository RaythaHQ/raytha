Here’s a tight Cursor prompt you can drop in. It tells the agent exactly what to migrate, how to wire routes/handlers, and what to fix on the BackgroundTaskStatus page.

---

**Cursor Prompt: Migrate MVC → Razor Pages for BeginExportToCsv and fix BackgroundTaskStatus backlink**

Goal
Migrate the MVC “BeginExportToCsv” flow to Razor Pages under `Pages/ContentTypes/Views/BeginExportToCsv`, preserving URL shape, permissions, and behavior. BeginExportToCsv must kick off the background job then redirect to `ContentTypes/Views/BackgroundTaskStatus`. Also correct `BackgroundTaskStatus`’s `BackLinkOptions.Page` to point back to the correct content items list page (the same target our existing “Back to List” uses for content items in this content type).

Scope

* Update Razor Page: `Pages/ContentTypes/Views/BeginExportToCsv.cshtml` and `BeginExportToCsv.cshtml.cs`.
* Update Razor Page: `Pages/ContentTypes/Views/BackgroundTaskStatus.cshtml(.cs)` for `BackLinkOptions.Page` fix.
* Follow our existing Razor Pages patterns: use our base PageModel if available, authorization attributes, service filters, TempData messaging helpers, and partials.

Functional requirements

1. Routing and auth

* The BeginExportToCsv page must respond to the same URL shape as legacy:
  `/{contentTypeDeveloperName}/views/{viewId}/export-to-csv`
* Use Razor Pages routing via `@page "/{contentTypeDeveloperName}/views/{viewId}/export-to-csv"`.
* Apply `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]` to the PageModel.
* Apply `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]` to the PageModel so `CurrentView` is available exactly like before.

2. GET handler

* On GET, render a form with radio buttons bound to `ViewColumnsOnly` defaulting to our ViewModel’s default.
* Page title should match legacy: `"{ContentTypeLabelPlural} > Export to CSV"`.
* Render `_PageHeader` with title and `Model.CurrentView.ContentTypeDescription`.
* Render the same “Back to List” partial the legacy page used, i.e., `_BackToList` using `ContentItemsBackToList_ViewModel` with `ContentTypeDeveloperName = CurrentView.ContentTypeDeveloperName` and `IsEditing = false`. Do not hardcode routes; use our existing VM and helpers.

3. POST handler

* On POST, send `BeginExportContentItemsToCsv.Command` with:

  * `ViewId = CurrentView.Id`
  * `ExportOnlyColumnsFromView = ViewColumnsOnly`
* On success: set the same success toast (“Export in progress.”) using our existing mechanism (`SetSuccessMessage` or TempData equivalent) and redirect to the BackgroundTaskStatus Razor Page with route values:
  `contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = response.Result`
* On failure: surface errors with our standard error message helper and redisplay the page.

4. Anti-forgery & form

* Use a plain `<form method="post">` so the Razor FormTagHelper emits the anti-forgery token automatically.
* Do not use the legacy `asp-route="contentitemscreate"`; that was wrong for this page.
* Keep Bootstrap classes and layout equivalent to the legacy snippet.

5. BackgroundTaskStatus fix

* Locate `Pages/ContentTypes/Views/BackgroundTaskStatus.cshtml(.cs)` where we build `BackLinkOptions`.
* `BackLinkOptions.Page` is currently wrong. Change it to the correct content items list page that our `_BackToList` uses for this content type. Concretely: set `BackLinkOptions.Page` to the same page constant or Razor Page path used elsewhere for “Back to List” of content items. For example, if we have a central routes helper or page constant for content items index, use that. If not, set it to the Razor Page path that lists content items for a content type, e.g. `"/{contentTypeDeveloperName}/content-items"` adapted to our actual page path.
* Preserve any existing `RouteValues` like `contentTypeDeveloperName = CurrentView.ContentType.DeveloperName`.

6. JSON handler parity (BackgroundTaskStatus)

* Ensure BackgroundTaskStatus supports a JSON variant consistent with legacy `bool json = false`. Implement an explicit handler like `OnGetJsonAsync` that returns the serialized result from `Mediator.Send(new GetBackgroundTaskById.Query { Id = id })` when `?json=true` is present. Otherwise return the normal page.

7. Code style & JS

* No Stimulus/Hotwire. No external frameworks.
* Keep any page-local JS inline in the `.cshtml` in a `<script>` tag at the bottom, following the pattern used by `Reorder.cshtml`.
* Keep things minimal; this page only needs the form submit.

Suggested file content and signatures

`Pages/ContentTypes/Views/BeginExportToCsv.cshtml`

* Top: `@page "/{contentTypeDeveloperName}/views/{viewId}/export-to-csv"`
* `@model BeginExportToCsvModel`
* Title in `ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > Export to CSV";`
* Render `_PageHeader` partial with Title and description.
* Render `_BackToList` as described.
* Form markup matching legacy layout with two radio buttons:

  * `asp-for="ViewColumnsOnly"` with values `true` and `false` and the same labels.
* Submit button text: “Begin export”.

`Pages/ContentTypes/Views/BeginExportToCsv.cshtml.cs`

```csharp
[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_READ_PERMISSION)]
[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
public class BeginExportToCsvModel : PageModelBase // use our base if present
{
    [BindProperty]
    public bool ViewColumnsOnly { get; set; } // default from VM if needed

    public async Task<IActionResult> OnGetAsync(string contentTypeDeveloperName, Guid viewId)
    {
        // Any initialization mirrors legacy – view already set by filter
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string contentTypeDeveloperName, Guid viewId)
    {
        var input = new BeginExportContentItemsToCsv.Command
        {
            ViewId = CurrentView.Id,
            ExportOnlyColumnsFromView = ViewColumnsOnly
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Export in progress.");
            return RedirectToPage("/ContentTypes/Views/BackgroundTaskStatus",
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, id = response.Result });
        }

        SetErrorMessage("There was an error attempting to begin this export. See the error below.", response.GetErrors());
        return Page();
    }
}
```

`Pages/ContentTypes/Views/BackgroundTaskStatus.cshtml.cs`

* Ensure it accepts `id` and optional `json` query.
* Add a dedicated handler `OnGetJsonAsync` that returns `JsonResult`.
* Fix `BackLinkOptions.Page` to the correct page for the content items list. Example:

```csharp
BackLinkOptions = new BackLinkOptions
{
    // Use our known good content items listing page for the current content type.
    Page = "/ContentTypes/ContentItems/Index", // or the project’s actual page constant/helper
    RouteValues = new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName }
};
```

* Do not change other behavior.

Acceptance criteria

* GET `/{contentTypeDeveloperName}/views/{viewId}/export-to-csv` renders the page, header, and “Back to List” link exactly like legacy.
* POST starts the export job, shows “Export in progress.”, then redirects to `/ {contentTypeDeveloperName}/background-task/status/{id}` Razor Page.
* `BackgroundTaskStatus` page’s Back link routes to the content items list page for the same content type.
* `?json=true` on BackgroundTaskStatus returns JSON payload from `GetBackgroundTaskById.Query`.
* Authorization and `GetOrSetRecentlyAccessedViewFilterAttribute` work on both pages.
* No Stimulus/Hotwire. Uses vanilla Razor Pages and our standard partials/helpers.
* Anti-forgery token is emitted and validated on POST.

Please implement, update us with the diff, and point to any project-specific route constants you used for the fixed `BackLinkOptions.Page`.
