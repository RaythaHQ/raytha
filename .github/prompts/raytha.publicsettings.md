You did a great job with porting over Sort. Look at my responses and guidance for your Sort task and keep consistent here

**Goal**
Port legacy MVC `ContentTypes/Views/PublicSettings` (view + controller actions) to a Razor Page using the same structure and conventions as `ContentTypes/Views/Edit`. Preserve URL shape, auth, filters, temp messages, and validation. Reuse existing helpers (e.g., `SetSuccessMessage`, `SetErrorMessage`) and keep the UI identical.

**Source to port**

* Legacy View: `ViewsPublicSettings_ViewModel` form with:

  * Template selector
  * RoutePath with WebsiteUrl prefix
  * IsPublished toggle
  * Default/Max items per page
  * IgnoreClientFilterAndSortQueryParams toggle
  * Save changes button
  * Conditional “Set as home page” block guarded by `MANAGE_SYSTEM_SETTINGS_PERMISSION`
  * Back link to `contentitemsindex`
* Controller actions:

  * GET `PublicSettings`
  * POST `PublicSettings` with antiforgery and validation re-population
  * POST `SetAsHomePage`
* Filters and policies:

  * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
  * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]` on page
  * `[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]` on SetAsHomePage only

**Target**

* New Razor Page at `Areas/Admin/Pages/ContentTypes/Views/PublicSettings.cshtml` with `PublicSettings.cshtml.cs`
* Route compatibility:

  * `/raytha/{contentTypeDeveloperName}/views/public-settings/{viewId}`
* Handlers:

  * `OnGetAsync`
  * `OnPostAsync` (save)
  * `OnPostSetAsHomePageAsync` (separate policy)

**Implementation details**

1. **Routing**

   * `@page "{contentTypeDeveloperName}/views/public-settings/{viewId:guid}/{handler?}"`
2. **PageModel decoration**

   * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
   * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]` on the class
   * Add `[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]` on the `OnPostSetAsHomePageAsync` handler method only
3. **Mediator calls**

   * Queries: `GetViewById.Query`, `GetWebTemplates.Query`, `GetWebTemplateByViewId.Query`
   * Commands: `EditPublicSettings.Command`, `SetAsHomePage.Command`
4. **Model properties**

   * Bind route and form fields; mirror the legacy ViewModel fields so the markup ports cleanly:

     * `Id`, `TemplateId`, `AvailableTemplates: Dictionary<string,string>`
     * `RoutePath`, `IsPublished`
     * `IgnoreClientFilterAndSortQueryParams`
     * `DefaultNumberOfItemsPerPage`, `MaxNumberOfItemsPerPage`
     * `WebsiteUrl`, `IsHomePage`
     * `ContentTypeDeveloperName` (get), `ViewId` (get)
   * If our base page (`RaythaPageModel`) exposes `CurrentView`, use that as the controller did
5. **GET flow**

   * Load view by id
   * Load available templates (current org theme + content type)
   * Load currently selected template id via `GetWebTemplateByViewId.Query`
   * Set `WebsiteUrl` using `CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/"` to match legacy
   * Set `IsHomePage` using `CurrentOrganization.HomePageId == CurrentView.Id`
6. **POST save flow**

   * Build `EditPublicSettings.Command` from bound props
   * On success: `SetSuccessMessage("Public settings updated successfully.")`, redirect back to this page (same route)
   * On failure: `SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());` repopulate `AvailableTemplates` and `WebsiteUrl`, return Page
7. **POST set-as-home-page**

   * Separate handler with stricter policy
   * On success: success toast; on failure: error toast; redirect back to this page
8. **Markup**

   * Keep `_PageHeader`, same labels, help text, Bootstrap classes
   * Keep the back link. If the target index is not yet a Razor Page, keep `asp-route="contentitemsindex"`; otherwise wire `asp-page="Index"` with the right route values
   * Keep validation classes using the same helpers if available. If helpers are extension methods on the legacy ViewModel, just keep the names and call sites; otherwise, replace with standard tag-helper validation (`asp-validation-for`) later as a clean-up
9. **Antiforgery**

   * Standard Razor Pages forms include the token by default. The legacy view posted with `[ValidateAntiForgeryToken]`—keep equivalent behavior by not ignoring antiforgery on `OnPostAsync`
10. **Consistency checks**

* Final URL paths unchanged
* Organization and View context resolved exactly like `ContentTypes/Views/Edit`

**Create these files**

`Areas/Admin/Pages/ContentTypes/Views/PublicSettings.cshtml`

```cshtml
@page "{contentTypeDeveloperName}/views/public-settings/{viewId:guid}/{handler?}"
@model Areas.Admin.Pages.ContentTypes.Views.PublicSettingsModel

@{
    ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > {Model.CurrentView.Label} > Public settings";
}

@(await Html.PartialAsync("_PageHeader", new PageHeader_ViewModel
{
    Title = ViewData["Title"]?.ToString(),
    Description = Model.CurrentView.ContentTypeDescription
}))

<div class="row mb-4">
  <div class="col-xxl-7 col-xl-8 col-lg-9 col-md-12">
    <div class="card border-0 shadow mb-4">
      <div class="card-body">

        <a asp-route="contentitemsindex"
           asp-route-viewId="@Model.CurrentView.Id"
           asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName">
          <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16l-4-4m0 0l4-4m-4 4h18"></path></svg>
          Back
        </a>

        <form method="post"
              asp-page-handler="Save"
              class="py-4"
              asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
              asp-route-viewId="@Model.Id">

          <div class="col-lg-12">
            <div class="mb-3">
              <label class="form-label raytha-required" asp-for="TemplateId"></label>
              <select class="form-select @(Model.HasError != null ? Model.HasError("TemplateId") : "")" asp-for="TemplateId">
                <option value="">-- SELECT --</option>
                @foreach (var template in Model.AvailableTemplates)
                {
                  <option value="@template.Key">@template.Value</option>
                }
              </select>
              <div class="invalid-feedback">@Model.ErrorMessageFor?.Invoke("TemplateId")</div>
            </div>
          </div>

          <div class="col-lg-12">
            <label class="form-label raytha-required" asp-for="RoutePath"></label>
            <div class="input-group mb-3">
              <span class="input-group-text">@Model.WebsiteUrl</span>
              <input type="text" class="form-control @(Model.HasError != null ? Model.HasError("RoutePath") : "")" asp-for="RoutePath" required>
              <div class="invalid-feedback">@Model.ErrorMessageFor?.Invoke("RoutePath")</div>
            </div>
          </div>

          <div class="col-lg-12">
            <div class="mb-3">
              <label class="form-label" asp-for="IsPublished"></label>
              <div class="form-check form-switch">
                <input class="form-check-input" type="checkbox" asp-for="IsPublished">
              </div>
              <div class="invalid-feedback">@Model.ErrorMessageFor?.Invoke("IsPublished")</div>
            </div>
          </div>

          <div class="col-lg-12 col-md-12">
            <div class="mb-3">
              <label class="form-label raytha-required" asp-for="DefaultNumberOfItemsPerPage"></label>
              <input type="number" class="form-control @(Model.HasError != null ? Model.HasError("DefaultNumberOfItemsPerPage") : "")" asp-for="DefaultNumberOfItemsPerPage" required>
              <div class="invalid-feedback">@Model.ErrorMessageFor?.Invoke("DefaultNumberOfItemsPerPage")</div>
            </div>
          </div>

          <div class="col-lg-12 col-md-12">
            <div class="mb-3">
              <label class="form-label raytha-required" asp-for="MaxNumberOfItemsPerPage"></label>
              <input type="number" class="form-control @(Model.HasError != null ? Model.HasError("MaxNumberOfItemsPerPage") : "")" asp-for="MaxNumberOfItemsPerPage" required>
              <div class="invalid-feedback">@Model.ErrorMessageFor?.Invoke("MaxNumberOfItemsPerPage")</div>
            </div>
          </div>

          <div class="col-lg-12 my-4">
            <div class="form-check">
              <input class="form-check-input" type="checkbox" asp-for="IgnoreClientFilterAndSortQueryParams">
              <label class="form-check-label" asp-for="IgnoreClientFilterAndSortQueryParams"></label>
            </div>
            <div class="form-text">
              Turn on this option if you do not want public consumers to be able to layer their own filtering or sorting on top of the view's configuration.'
            </div>
          </div>

          <input type="hidden" asp-for="Id" />
          <div class="col-lg-12">
            <button type="submit" class="btn btn-success mx-2" name="SaveAsDraft" value="false">Save changes</button>
          </div>
        </form>

        <br />

        @if (Model.CanManageSystemSettings)
        {
          <hr />
          if (!Model.IsHomePage)
          {
            <form method="post"
                  asp-page-handler="SetAsHomePage"
                  class="py-4"
                  asp-route-contentTypeDeveloperName="@Model.CurrentView.ContentTypeDeveloperName"
                  asp-route-viewId="@Model.Id">
              <div class="col-lg-12">
                <button type="submit" class="btn btn-secondary mx-2">Set as home page</button>
              </div>
            </form>
          }
          else
          {
            <p>This list view is currently set as the home page.</p>
          }
        }

      </div>
    </div>
  </div>
</div>
```

`Areas/Admin/Pages/ContentTypes/Views/PublicSettings.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Raytha.Application.Views.Queries;
using Raytha.Application.WebTemplates.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Application.WebTemplates.Queries.Models; // if needed
using System.ComponentModel.DataAnnotations;

namespace Areas.Admin.Pages.ContentTypes.Views
{
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    public class PublicSettingsModel : RaythaPageModel
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorization;

        public PublicSettingsModel(IMediator mediator, IAuthorizationService authorization)
        {
            _mediator = mediator;
            _authorization = authorization;
        }

        // Route params
        [BindProperty(SupportsGet = true)]
        public string ContentTypeDeveloperName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ViewId { get; set; }

        // Form + display data
        [BindProperty] public Guid Id { get; set; }
        [BindProperty] public string RoutePath { get; set; }
        [BindProperty] public bool IsPublished { get; set; }
        [BindProperty] public string TemplateId { get; set; }
        [BindProperty] public bool IgnoreClientFilterAndSortQueryParams { get; set; }
        [BindProperty] public int DefaultNumberOfItemsPerPage { get; set; }
        [BindProperty] public int MaxNumberOfItemsPerPage { get; set; }

        public Dictionary<string, string> AvailableTemplates { get; set; } = new();
        public string WebsiteUrl { get; set; }
        public bool IsHomePage { get; set; }
        public bool CanManageSystemSettings { get; set; }

        // Optional helpers to mimic legacy validation helpers in markup
        public Func<string, string> HasError => field =>
            ViewData.ModelState.TryGetValue(field, out var entry) && entry.Errors.Any() ? "is-invalid" : string.Empty;

        public Func<string, string> ErrorMessageFor => field =>
            ViewData.ModelState.TryGetValue(field, out var entry) && entry.Errors.Any()
                ? string.Join(" ", entry.Errors.Select(e => e.ErrorMessage))
                : string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // Load core data
            var viewResponse = await _mediator.Send(new GetViewById.Query { Id = CurrentView.Id });

            var webTemplates = await _mediator.Send(new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue
            });

            var webTemplateIdResponse = await _mediator.Send(new GetWebTemplateByViewId.Query
            {
                ViewId = CurrentView.Id,
                ThemeId = CurrentOrganization.ActiveThemeId
            });

            Id = viewResponse.Result.Id;
            RoutePath = viewResponse.Result.RoutePath;
            IsPublished = viewResponse.Result.IsPublished;
            TemplateId = webTemplateIdResponse.Result.Id;
            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label) ?? new();
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
            IgnoreClientFilterAndSortQueryParams = viewResponse.Result.IgnoreClientFilterAndSortQueryParams;
            MaxNumberOfItemsPerPage = viewResponse.Result.MaxNumberOfItemsPerPage;
            DefaultNumberOfItemsPerPage = viewResponse.Result.DefaultNumberOfItemsPerPage;
            IsHomePage = CurrentOrganization.HomePageId == CurrentView.Id;

            var auth = await _authorization.AuthorizeAsync(User, BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);
            CanManageSystemSettings = auth.Succeeded;

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            var input = new EditPublicSettings.Command
            {
                Id = Id,
                RoutePath = RoutePath,
                IsPublished = IsPublished,
                TemplateId = TemplateId,
                IgnoreClientFilterAndSortQueryParams = IgnoreClientFilterAndSortQueryParams,
                MaxNumberOfItemsPerPage = MaxNumberOfItemsPerPage,
                DefaultNumberOfItemsPerPage = DefaultNumberOfItemsPerPage
            };

            var response = await _mediator.Send(input);
            if (response.Success)
            {
                SetSuccessMessage("Public settings updated successfully.");
                return RedirectToPage("PublicSettings",
                    new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
            }

            SetErrorMessage("There were validation errors with your form submission. Please correct the fields below.", response.GetErrors());

            // Repopulate lists and display-only fields
            var webTemplates = await _mediator.Send(new GetWebTemplates.Query
            {
                ThemeId = CurrentOrganization.ActiveThemeId,
                ContentTypeId = CurrentView.ContentTypeId,
                PageSize = int.MaxValue
            });

            AvailableTemplates = webTemplates.Result?.Items.ToDictionary(p => p.Id.ToString(), p => p.Label) ?? new();
            WebsiteUrl = CurrentOrganization.WebsiteUrl.TrimEnd('/') + CurrentOrganization.PathBase + "/";
            IsHomePage = CurrentOrganization.HomePageId == CurrentView.Id;

            var auth = await _authorization.AuthorizeAsync(User, BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION);
            CanManageSystemSettings = auth.Succeeded;

            return Page();
        }

        [Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSetAsHomePageAsync()
        {
            var input = new SetAsHomePage.Command { Id = CurrentView.Id };
            var response = await _mediator.Send(input);
            if (response.Success)
                SetSuccessMessage("Home page updated successfully.");
            else
                SetErrorMessage("There was an error setting this as the home page.", response.GetErrors());

            return RedirectToPage("PublicSettings",
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id });
        }
    }
}
```

**What to double-check**

* Area and page namespace align with your existing Razor Pages setup
* The “Back” link still points to `contentitemsindex` until that page is ported
* The `TemplateId` type is `string` in legacy select; keep as `string` unless domain expects `Guid`
* `WebsiteUrl` construction matches current org rules
* Handler-level authorization for SetAsHomePage works in your ASP.NET Core version; if not, use a custom `IAuthorizationService` check in the handler and return `Forbid()` when unauthorized

**Deliverables**

* The two files above compiled and routed to `/raytha/{contentTypeDeveloperName}/views/public-settings/{viewId}`
* Legacy controller actions removed once this page is live
* Visual parity with the old view and identical side effects on save and home-page update
