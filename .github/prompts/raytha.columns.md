You did a great job with porting over Sort. Look at my responses and guidance for your Sort task and keep consistent here
---

**Goal**
Port legacy MVC `ContentTypes/Views/Columns` (view + three actions) to a Razor Page. Keep URL shape, auth, filters, toast messaging, and the vanilla drag-drop module used elsewhere. Follow the same structure you used for `ContentTypes/Views/Sort` and `.../PublicSettings`, and mirror the organization found in `ContentTypes/Views/Edit`.

**Source to port**

* Actions:

  * GET `Columns`
  * PATCH `ColumnsReorderAjax` (AJAX)
  * POST `ColumnsToggle` with form field `action` = `add` or `remove`
* View:

  * Two lists: Unselected and Selected
  * Selected list uses the shared reorder JS via `data-controller="shared--reorderlist"` and `data-sortable-update-url`
  * Back link to `contentitemsindex`
* Filters and policies:

  * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
  * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]`

**Target**

* Razor Page at `Areas/Admin/Pages/ContentTypes/Views/Columns.cshtml` with backing `Columns.cshtml.cs`
* Compatible routes:

  * GET `/raytha/{contentTypeDeveloperName}/views/{viewId}/columns`
  * PATCH `/raytha/{contentTypeDeveloperName}/views/{viewId}/columns/reorder/{developerName}`
  * POST `/raytha/{contentTypeDeveloperName}/views/{viewId}/columns/toggle/{developerName}`
* Handlers:

  * `OnGetAsync`
  * `OnPostToggleAsync(string developerName)` for add/remove
  * `OnPostReorderAsync(string developerName)` for AJAX drag-drop reorder

**Implementation details**

1. **Routing**

   * `@page "{contentTypeDeveloperName}/views/{viewId:guid}/columns/{handler?}/{developerName?}"`
2. **PageModel decoration**

   * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
   * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]`
3. **Mediator + domain**

   * Queries: `GetContentTypeFields.Query`
   * Commands: `ReorderColumn.Command`, `EditColumn.Command`
   * Use `CurrentView`, `CurrentOrganization`, and `SortOrder` exactly as you did in `Sort`
4. **Model**

   * Keep the list-item type `ViewsColumnsListItem_ViewModel`
   * PageModel properties:

     * Route: `ContentTypeDeveloperName`, `ViewId`
     * Data: `ViewsColumnsListItem_ViewModel[] SelectedColumns`, `ViewsColumnsListItem_ViewModel[] NotSelectedColumns`
5. **GET logic**

   * Same as legacy: build combined list of content type fields + built-ins, mark Selected by `CurrentView.Columns.Contains(p.DeveloperName)`, compute `FieldOrder` from `CurrentView.Columns`
   * Split into `SelectedColumns` ordered by `FieldOrder`, and `NotSelectedColumns` ordered by `DeveloperName`
6. **POST toggle**

   * Read `action` from `Request.Form["action"]`
   * `ShowColumn = action == "add"`
   * On failure, `SetErrorMessage("There was an error adding this column to the view", response.GetErrors())`
   * Redirect back to Columns page with the same route
7. **AJAX reorder**

   * Read `position` from `Request.Form["position"]`
   * Execute `ReorderColumn.Command`
   * If failure, set `Response.StatusCode = 400`
   * Return `new JsonResult(response)`
   * Use `[IgnoreAntiforgeryToken]` here if our shared reorder JS does not send the token, same as in `Sort`
8. **Markup**

   * Keep `_PageHeader` and the Notyf CSS include
   * Back link stays to `contentitemsindex` until that page is ported
   * Replace `Url.Action("ColumnsReorderAjax", ...)` with `Url.Page("Columns", "Reorder", new { ... })`
   * Replace form routes for toggle with `asp-page-handler="Toggle"` and proper route values

**Create these files**

`Areas/Admin/Pages/ContentTypes/Views/Columns.cshtml`

```cshtml
@page "{contentTypeDeveloperName}/views/{viewId:guid}/columns/{handler?}/{developerName?}"
@model Areas.Admin.Pages.ContentTypes.Views.ColumnsModel
@{
    ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > {Model.CurrentView.Label} > Columns";
}
@section headstyles
{
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

        <a asp-route="contentitemsindex"
           asp-route-viewId="@Model.CurrentView.Id"
           asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName">
          <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16l-4-4m0 0l4-4m-4 4h18"></path></svg>
          Back
        </a>

        <div class="row">
          <div class="col-md-6">
            <h4 class="pt-2">Unselected columns</h4>
            <ul class="list-group mt-2">
              @if (Model.NotSelectedColumns?.Any() == true)
              {
                foreach (var item in Model.NotSelectedColumns)
                {
                  <li class="list-group-item border d-flex justify-content-between">
                    <div>
                      @item.Label <small>@item.DeveloperName</small>
                    </div>
                    <form method="post"
                          asp-page-handler="Toggle"
                          asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
                          asp-route-viewId="@Model.CurrentView.Id"
                          asp-route-developerName="@item.DeveloperName">
                      <button class="btn btn-link btn-xs">
                        <svg class="icon icon-xs" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6"></path></svg>
                      </button>
                      <input type="hidden" name="action" value="add" />
                    </form>
                  </li>
                }
              }
              else
              {
                <p>All columns have been selected.</p>
              }
            </ul>
          </div>

          <div class="col-md-6">
            <h4 class="pt-2">Selected columns</h4>
            <ul data-controller="shared--reorderlist" data-shared--reorderlist-animation-value="150"
                class="list-group mt-2">
              @if (Model.SelectedColumns?.Any() == true)
              {
                foreach (var item in Model.SelectedColumns)
                {
                  <li data-sortable-update-url="@Url.Page("Columns", "Reorder",
                        new { contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName,
                              viewId = Model.CurrentView.Id, developerName = item.DeveloperName })"
                      class="list-group-item border raytha-draggable d-flex justify-content-between">
                    <div class="col-10">
                      <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="icon icon-xs me-2">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 9h16.5m-16.5 6.75h16.5" />
                      </svg>
                      @item.Label <small>@item.DeveloperName</small>
                    </div>
                    <form method="post"
                          asp-page-handler="Toggle"
                          asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
                          asp-route-viewId="@Model.CurrentView.Id"
                          asp-route-developerName="@item.DeveloperName">
                      <button class="text-danger btn btn-link btn-xs">
                        <svg class="icon icon-sm mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                      </button>
                      <input type="hidden" name="action" value="remove" />
                    </form>
                  </li>
                }
              }
            </ul>
          </div>
        </div>

      </div>
    </div>
  </div>
</div>
```

`Areas/Admin/Pages/ContentTypes/Views/Columns.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Domain.ValueObjects;

namespace Areas.Admin.Pages.ContentTypes.Views
{
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    public class ColumnsModel : RaythaPageModel
    {
        private readonly IMediator _mediator;

        public ColumnsModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string ContentTypeDeveloperName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ViewId { get; set; }

        public ViewsColumnsListItem_ViewModel[] SelectedColumns { get; set; }
        public ViewsColumnsListItem_ViewModel[] NotSelectedColumns { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var response = await _mediator.Send(new GetContentTypeFields.Query
            {
                PageSize = int.MaxValue,
                OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
                DeveloperName = CurrentView.ContentType.DeveloperName
            });

            var columnListItems = response.Result.Items.Select(p => new ViewsColumnsListItem_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Columns.Contains(p.DeveloperName),
                FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName)
            }).ToList();

            var builtInListItems = BuiltInContentTypeField.ReservedContentTypeFields.Select(p => new ViewsColumnsListItem_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                Selected = CurrentView.Columns.Contains(p.DeveloperName),
                FieldOrder = CurrentView.Columns.ToList().IndexOf(p.DeveloperName)
            }).ToList();

            columnListItems.AddRange(builtInListItems);

            var selectedColumns = columnListItems.Where(p => p.Selected).OrderBy(c => c.FieldOrder);
            var notSelectedColumns = columnListItems.Where(p => !p.Selected).OrderBy(c => c.DeveloperName);

            SelectedColumns = selectedColumns.ToArray();
            NotSelectedColumns = notSelectedColumns.ToArray();

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostToggleAsync(string developerName)
        {
            var action = Request.Form["action"].ToString();
            var command = new EditColumn.Command
            {
                Id = CurrentView.Id,
                DeveloperName = developerName,
                ShowColumn = action == "add"
            };

            var response = await _mediator.Send(command);
            if (!response.Success)
                SetErrorMessage("There was an error adding this column to the view", response.GetErrors());

            return RedirectToPage("Columns", new
            {
                contentTypeDeveloperName = CurrentView.ContentType.DeveloperName,
                viewId = CurrentView.Id
            });
        }

        // Match legacy AJAX reorder behavior. Use IgnoreAntiforgeryToken if our JS does not send the token.
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostReorderAsync(string developerName)
        {
            var position = Request.Form["position"];
            var input = new ReorderColumn.Command
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

**Double-check**

* Area namespace and base class match your project
* The reorder JS uses the same dataset attributes and still works with the new URLs
* Antiforgery: keep token on toggle, ignore for reorder if consistent with `Sort`
* Keep `contentitemsindex` route until that page is ported

**Deliverables**

* Two files above added
* Old Columns controller routes removed after verification
* Visual and behavioral parity with legacy Columns page
