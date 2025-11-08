**Goal**
Convert the legacy MVC View + Controller for “Views > Sort” into a single Razor Pages endpoint using our existing patterns. Preserve behavior, URLs, and permissions. Reuse our existing vanilla JS drag-drop module used elsewhere. Follow the same code organization as `ContentTypes/Views/Edit`.

**Source to port**

* View: the `.cshtml` snippet I pasted (uses `ViewsSort_ViewModel`, `_PageHeader`, vanilla drag-drop via `data-controller="shared--reorderlist"` and `data-sortable-update-url`)
* Controller actions: `Sort`, `SortRemove`, `SortAdd`, `SortReorderAjax` shown above
* Filters and policies used:

  * `GetOrSetRecentlyAccessedViewFilterAttribute`
  * `Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)`

**Target**

* New Razor Page at `Areas/Admin/Pages/ContentTypes/Views/Sort.cshtml` with backing model `Sort.cshtml.cs`
* Keep route shape compatible:

  * GET `/raytha/{contentTypeDeveloperName}/views/{viewId}/sort`
  * POST remove `/raytha/{contentTypeDeveloperName}/views/{viewId}/sort/remove/{developerName}`
  * POST add `/raytha/{contentTypeDeveloperName}/views/{viewId}/sort/add`
  * AJAX reorder `/raytha/{contentTypeDeveloperName}/views/{viewId}/sort/reorder/{developerName}`
* Use page handlers instead of separate controllers: `OnGet`, `OnPostRemoveAsync`, `OnPostAddAsync`, `OnPostReorderAsync`
* Keep authorization and the “recently accessed view” filter applied to the page
* Reuse the existing shared vanilla JS reorder module already in the project. Do not add Stimulus

**Implementation details**

1. **Page routing**

   * Top of page: `@page "{contentTypeDeveloperName}/views/{viewId}/sort/{handler?}/{developerName?}"`
   * Constrain types if we normally do so for IDs
2. **Filters and auth**

   * Decorate PageModel with `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]` and `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]`
3. **Access to CurrentView**

   * Use the same approach as other Razor Pages that need `CurrentView` (check our base `RaythaPageModel` or similar). If we do not have one, inject the same services the controller relied on and resolve `CurrentView` consistently with how `ContentTypes/Views/Edit` does it
4. **Mediator calls**

   * Port the exact mediator queries/commands:

     * `GetContentTypeFields.Query`
     * `EditSort.Command` for add/remove
     * `ReorderSort.Command` for reorder
5. **ViewModel**

   * Replace the legacy `ViewsSort_ViewModel` with PageModel properties:

     ```csharp
     public class SortModel : RaythaPageModel // or PageModel, matching our convention
     {
         [BindProperty(SupportsGet = true)]
         public string ContentTypeDeveloperName { get; set; }
         [BindProperty(SupportsGet = true)]
         public Guid ViewId { get; set; }

         public ViewsSortListItem_ViewModel[] SelectedColumns { get; set; }
         public Dictionary<string, string> NotSelectedColumns { get; set; }

         // For POST add
         [BindProperty]
         public string DeveloperName { get; set; }
         [BindProperty]
         public string OrderByDirection { get; set; } = SortOrder.ASCENDING;

         // For POST reorder
         [BindProperty(SupportsGet = true)]
         public string DeveloperNameRoute { get; set; }
     }
     ```

     Use the same `ViewsSortListItem_ViewModel` type already in the project
6. **Handlers**

   * `OnGet` reproduces the old `Sort` action logic to populate `SelectedColumns` and `NotSelectedColumns`
   * `OnPostRemoveAsync(string developerName)` reproduces `SortRemove`, sets error message on failure, then redirects to `OnGet`
   * `OnPostAddAsync()` reads bound `DeveloperName` and `OrderByDirection`, reproduces `SortAdd`, sets error message on failure, then redirects to `OnGet`
   * `OnPostReorderAsync(string developerName)` accepts `position` from `Request.Form["position"]`, executes `ReorderSort.Command`, returns `new JsonResult(response)` and sets status code 400 when `!response.Success`
   * Add `[ValidateAntiForgeryToken]` to form posts. For AJAX reorder keep antiforgery consistent with how we did drag-drop elsewhere. If our shared module already sends the token header, verify it. If we previously allowed it without token, annotate reorder handler with `[IgnoreAntiforgeryToken]` to match existing behavior
7. **Markup port**

   * Keep `_PageHeader` partial usage
   * Keep Notyf CSS include in `headstyles`
   * Replace `asp-route` and `Url.Action` usages with `asp-page` and `asp-page-handler` equivalents. Generate URLs with `Url.Page` for `data-sortable-update-url`
   * Keep all Bootstrap classes and icons as is
8. **Reorder URL wiring**

   * Replace:

     ```cshtml
     data-sortable-update-url="@Url.Action("SortReorderAjax", new { viewId = Model.CurrentView.Id, contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName, developerName = item.DeveloperName })"
     ```

     with:

     ```cshtml
     data-sortable-update-url="@Url.Page("Sort", pageHandler: "Reorder",
         values: new { contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName, viewId = Model.CurrentView.Id, developerName = item.DeveloperName })"
     ```
9. **Remove/Add form actions**

   * Remove:

     ```cshtml
     asp-route="viewssortremove"
     ```

     Use:

     ```cshtml
     asp-page="Sort" asp-page-handler="Remove"
     asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
     asp-route-viewId="@Model.CurrentView.Id"
     asp-route-developerName="@item.DeveloperName"
     ```
   * Add:

     ```cshtml
     <form asp-page="Sort" asp-page-handler="Add"
           asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
           asp-route-viewId="@Model.CurrentView.Id" method="post">
     ```
10. **Redirects**

    * Use:

      ```csharp
      return RedirectToPage("Sort", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
      ```
11. **Reuse existing JS**

    * Do not change our shared reorder JS. Only ensure the `data-sortable-update-url` points to the new handler
12. **Tests and acceptance**

    * GET renders the same list of Selected and NotSelected columns and the same order
    * Add and Remove mutate sort and redirect back to GET
    * Drag-drop fires AJAX to the new handler and persists order
    * All routes resolve under `/raytha/...` exactly like before
    * Authorization and “recently accessed view” behavior preserved

**Create these files**

`Areas/Admin/Pages/ContentTypes/Views/Sort.cshtml`

```cshtml
@page "{contentTypeDeveloperName}/views/{viewId:guid}/sort/{handler?}/{developerName?}"
@model Areas.Admin.Pages.ContentTypes.Views.SortModel
@using Raytha.Domain.ValueObjects
@{
    ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > {Model.CurrentView.Label} > Sort";
}
@section headstyles {
    <link rel="stylesheet" href="~/raytha_admin/css/notyf.min.css" />
}

@(await Html.PartialAsync("_PageHeader", new PageHeader_ViewModel
{
    Title = ViewData["Title"]?.ToString(),
    Description = Model.CurrentView.ContentTypeDescription
}))

<div class="row mb-4">
  <div class="col-lg-12 col-md-12">
    <div class="card border-0 shadow mb-4">
      <div class="card-body">

        <a asp-page="Index"
           asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
           asp-route-viewId="@Model.CurrentView.Id">
          <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16l-4-4m0 0l4-4m-4 4h18"></path></svg>
          Back
        </a>

        <div class="row">
          <div class="col-md-6">
            <h4 class="pt-2">Sorted columns</h4>

            @if (Model.SelectedColumns?.Any() == true)
            {
              <ul data-controller="shared--reorderlist" data-shared--reorderlist-animation-value="150"
                  class="list-group mt-2">
                @foreach (var item in Model.SelectedColumns)
                {
                  <li data-sortable-update-url="@Url.Page("Sort", "Reorder",
                                      new { contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName,
                                            viewId = Model.CurrentView.Id,
                                            developerName = item.DeveloperName })"
                      class="list-group-item border raytha-draggable d-flex justify-content-between">
                    <div class="col-10">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon icon-xs me-2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 9h16.5m-16.5 6.75h16.5" />
                      </svg>
                      @item.Label <small>@item.DeveloperName</small>
                    </div>
                    <div class="col-1">
                      @item.OrderByDirection
                    </div>
                    <form method="post"
                          asp-page="Sort" asp-page-handler="Remove"
                          asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
                          asp-route-viewId="@Model.CurrentView.Id"
                          asp-route-developerName="@item.DeveloperName">
                      <button class="text-danger btn btn-link btn-xs">
                        <svg class="icon icon-sm mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                      </button>
                    </form>
                  </li>
                }
              </ul>
            }

            <div class="col-lg-12">
              <div class="mb-3">
                <form method="post"
                      asp-page="Sort" asp-page-handler="Add"
                      asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
                      asp-route-viewId="@Model.CurrentView.Id"
                      class="py-4">
                  <div class="col-lg-12 col-md-6">
                    <label class="form-label">Add a column to sort by</label>
                    <div class="row">
                      <div class="col-6">
                        <select class="form-select" asp-for="DeveloperName" name="DeveloperName" required>
                          <option value="">-- SELECT --</option>
                          @foreach (var field in Model.NotSelectedColumns)
                          {
                            <option value="@field.Key">@field.Key</option>
                          }
                        </select>
                      </div>
                      <div class="col-6">
                        <select class="form-select" asp-for="OrderByDirection" name="OrderByDirection">
                          <option value="asc">Ascending</option>
                          <option value="desc">Descending</option>
                        </select>
                      </div>
                    </div>
                  </div>
                  <button type="submit" class="btn btn-success mt-4">
                    <svg class="icon icon-xs" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg>
                    Add column
                  </button>
                </form>
              </div>
            </div>

          </div>
        </div>

      </div>
    </div>
  </div>
</div>
```

`Areas/Admin/Pages/ContentTypes/Views/Sort.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Domain.ValueObjects;

namespace Areas.Admin.Pages.ContentTypes.Views
{
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    public class SortModel : RaythaPageModel // match our base class
    {
        private readonly IMediator _mediator;

        public SortModel(IMediator mediator /*, inject any other services CurrentView requires */)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string ContentTypeDeveloperName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ViewId { get; set; }

        public ViewsSortListItem_ViewModel[] SelectedColumns { get; set; }
        public Dictionary<string, string> NotSelectedColumns { get; set; }

        [BindProperty] public string DeveloperName { get; set; }
        [BindProperty] public string OrderByDirection { get; set; } = SortOrder.ASCENDING;

        // If you need it for route binding on reorder
        [BindProperty(SupportsGet = true)]
        public string DeveloperNameRoute { get; set; }

        public async Task<IActionResult> OnGet()
        {
            // Ensure CurrentView is populated the same way as in Edit page
            var response = await _mediator.Send(new GetContentTypeFields.Query
            {
                PageSize = int.MaxValue,
                OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
                DeveloperName = CurrentView.ContentType.DeveloperName
            });

            var columnListItems = response.Result.Items.Select(p => new ViewsSortListItem_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
                FieldOrder = CurrentView.Sort.Select(x => x.DeveloperName).ToList().IndexOf(p.DeveloperName),
                OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName)
                    ? CurrentView.Sort.First(c => c.DeveloperName == p.DeveloperName).SortOrder.DeveloperName
                    : SortOrder.ASCENDING
            }).ToList();

            var builtInListItems = BuiltInContentTypeField.ReservedContentTypeFields.Select(p => new ViewsSortListItem_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName),
                FieldOrder = CurrentView.Sort.Select(x => x.DeveloperName).ToList().IndexOf(p.DeveloperName),
                OrderByDirection = CurrentView.Sort.Any(c => c.DeveloperName == p.DeveloperName)
                    ? CurrentView.Sort.First(c => c.DeveloperName == p.DeveloperName).SortOrder.DeveloperName
                    : SortOrder.ASCENDING
            }).ToList();

            columnListItems.AddRange(builtInListItems);

            var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
            var notSelectedColumns = columnListItems.Where(p => !p.Selected).OrderBy(c => c.DeveloperName);

            SelectedColumns = selectedColumns.ToArray();
            NotSelectedColumns = notSelectedColumns.ToDictionary(p => p.DeveloperName, p => p.Label);

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAsync(string developerName)
        {
            var command = new EditSort.Command
            {
                Id = CurrentView.Id,
                DeveloperName = developerName,
                ShowColumn = false
            };

            var response = await _mediator.Send(command);
            if (!response.Success)
                SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

            return RedirectToPage("Sort", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddAsync()
        {
            var command = new EditSort.Command
            {
                Id = CurrentView.Id,
                DeveloperName = DeveloperName,
                ShowColumn = true,
                OrderByDirection = OrderByDirection
            };

            var response = await _mediator.Send(command);
            if (!response.Success)
                SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

            return RedirectToPage("Sort", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
        }

        // Use IgnoreAntiforgeryToken if our drag-drop module does not submit the token
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostReorderAsync(string developerName)
        {
            var position = Request.Form["position"];
            var input = new ReorderSort.Command
            {
                Id = CurrentView.Id,
                DeveloperName = developerName,
                NewFieldOrder = Convert.ToInt32(position)
            };

            var response = await _mediator.Send(input);
            if (!response.Success)
                Response.StatusCode = StatusCodes.Status400BadRequest;

            return new JsonResult(response);
        }
    }
}
```

**What to double-check**

* Route prefix `RAYTHA_ROUTE_PREFIX` is implied by our area configuration. If needed, add a route template prefix on the page or configure in Area routing so final URL stays `/raytha/...`
* The “Back” link should match the previous named route `contentitemsindex`. If we have a Razor Page for that, use the correct page name. If not, keep the route name via `asp-route` to existing MVC route until that page is also migrated
* Antiforgery handling in the reorder AJAX matches our existing drag-drop implementation
* Keep UI text and icons unchanged

**Deliverables**

* The two files above compiled and wired
* Removal of the old controller routes for Sort once this page is live
* Unit or integration test updated to hit Razor Page routes instead of controller

Do the conversion now and keep code style identical to `ContentTypes/Views/Edit`.
