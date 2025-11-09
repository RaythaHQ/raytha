Got it. Here’s a tight Cursor prompt that drafts **all 8 steps** and makes it crystal clear that the agent must **execute one step at a time** and **wait for your review** before proceeding.

---

## Cursor Prompt — Migrate `ContentItems/Create` and `ContentItems/Edit` to Razor Pages

You are refactoring legacy MVC views and controllers into Razor Pages for Raytha CMS. The stubs already exist for `ContentItems/Create` and `ContentItems/Edit`. Follow Clean Architecture and Razor Pages best practices. Remove all Stimulus/Hotwire. Use only vanilla JavaScript. Preserve functionality including file upload attachment field, TipTap WYSIWYG, and one to one relationship typeahead/lookup.

**Important workflow rule:** execute **exactly one step** below, then stop. Output a short summary of what changed and a file-by-file diff. Wait for human review before moving to the next step.

### Global constraints

* Clean Architecture. Keep PageModels thin. Orchestrate via MediatR. Do not push domain logic into the UI.
* Reuse: extract shared logic into a base PageModel and shared partials.
* Routes must remain compatible with existing URLs.
* No Stimulus, no Hotwire. Vanilla JS modules placed in `wwwroot/js/contentitems/`.
* Keep existing partials like `_PageHeader`, `_BackToList`, `_ActionsMenu`, `_Autocomplete`. You may add new partials for field rendering.
* Guard against regressions in draft vs publish flows, validation, and authorization.
* Do not introduce new bugs.

---

## Step 1 — PageModel foundations and routing

**Scope:** set up PageModels and routes for Create and Edit, no UI changes yet.

**Files to add or modify**

* `Pages/ContentItems/Create.cshtml.cs`
* `Pages/ContentItems/Edit.cshtml.cs`
* `Pages/ContentItems/BaseContentItemsPageModel.cs` (new)

**Implementation**

* Create `BaseContentItemsPageModel` with DI for `IMediator`, `IAuthorizationService`.
* Add helpers:

  * `PopulateTemplatesAsync(org, view, vm)`
  * `PopulateMediaItemsAsync(vm)` to fetch image/video lists and serialize to JSON.
  * `BuildFieldValuesForCreate(view)` to shape `FieldValues` for initial render.
  * `BuildFieldValuesForEdit(view, contentItem)` to shape `FieldValues` for edit.
* Apply `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]` on both PageModels.
* `Create.cshtml.cs`: support `OnGetAsync()` and `OnPostAsync()` for create flow. Accept `contentTypeDeveloperName` via route.
* `Edit.cshtml.cs`: support `OnGetAsync(string id, string backToListUrl)` and `OnPostAsync(string id)` for edit flow.
* Preserve route templates:

  * `Create`: `@page "/raytha/{contentTypeDeveloperName}/create"`
  * `Edit`: `@page "/raytha/{contentTypeDeveloperName}/edit/{id}"`

**Acceptance checklist**

* Build passes.
* Hitting the routes runs PageModel methods without rendering changes yet.
* No Stimulus or Hotwire introduced.

**Stop after Step 1. Output summary and diffs. Wait.**

---

## Step 2 — Shared partials and field rendering skeleton

**Scope:** create reusable partials to remove duplication. No business behavior changes.

**Files to add**

* `Pages/ContentItems/Shared/_FieldRenderer.cshtml`
* `Pages/ContentItems/Shared/_AttachmentField.cshtml`
* `Pages/ContentItems/Shared/_WysiwygField.cshtml`
* `Pages/ContentItems/Shared/_RelationshipField.cshtml`
* `Pages/ContentItems/Shared/_FieldReadonly.cshtml`

**Implementation**

* `_FieldRenderer` switches on `Model.FieldType` to render simple inputs:

  * `single_line_text`, `number`, `checkbox`, `long_text`, `date`, `dropdown`, `radio`, `multiple_select`
* Defer complex types to their own partials:

  * `attachment` → `_AttachmentField`
  * `wysiwyg` → `_WysiwygField`
  * `one_to_one_relationship` → `_RelationshipField`
* `_FieldReadonly` renders label and value as static markup for read only views.

**Acceptance checklist**

* Both Create/Edit pages can iterate fields and call `_FieldRenderer` without Stimulus.
* Build compiles. No JS yet.

**Stop after Step 2. Output summary and diffs. Wait.**

---

## Step 3 — Move controller logic into PageModels

**Scope:** parity with MVC controller logic inside `OnGetAsync`/`OnPostAsync`, using existing ViewModels.

**Implementation**

* `Create.OnGetAsync`: call `GetWebTemplates`, `GetMediaItems` twice, build `FieldValues` via helper. Fill in `AllowedMimeTypes`, `MaxFileSize`, `UseDirectUploadToCloud`, `PathBase`, and media JSONs.
* `Create.OnPostAsync`:

  * Map `FieldValues` to `CreateContentItem.Command` via a mapper.
  * Handle `SaveAsDraft`, `TemplateId`.
  * On success, redirect to Edit same as legacy.
  * On failure, rebuild choices and values like legacy.
* `Edit.OnGetAsync`:

  * `GetContentItemById`, `GetMediaItems` twice.
  * Build `FieldValues` using `FieldValueConverter` equivalents.
  * Set `IsDraft`, `IsPublished`, `BackToListUrl`.
* `Edit.OnPostAsync`:

  * Map `FieldValues` to `EditContentItem.Command`, preserve draft flag logic, handle errors as legacy.

**Routing note**

* Replace controller `RedirectToAction` with `RedirectToPage` keeping same route segments.

**Acceptance checklist**

* Feature parity with previous controller logic.
* All server side validation messages appear in the model state.

**Stop after Step 3. Output summary and diffs. Wait.**

---

## Step 4 — Razor markup migration in Create/Edit

**Scope:** update `Create.cshtml` and `Edit.cshtml` to use partials and tag helpers cleanly.

**Implementation**

* Keep `_PageHeader`, `_BackToList`, `_ActionsMenu` usage.
* Replace inline field loops with:

  ```razor
  @for (var i = 0; i < Model.ViewModel.FieldValues.Count; i++)
  {
      <partial name="Shared/_FieldRenderer" model="Model.ViewModel.FieldValues[i]" />
      <input type="hidden" asp-for="ViewModel.FieldValues[i].DeveloperName" />
      <input type="hidden" asp-for="ViewModel.FieldValues[i].FieldType" />
  }
  ```
* Use `asp-page` for forms:

  * `Create` form posts to same page. Include two submit buttons with `name="SaveAsDraft"` values `true` and `false`.
  * `Edit` respects readonly mode. If unauthorized to edit, render using `_FieldReadonly` partials inside a non posting form stub.
* Keep anti forgery tokens and validation summary.

**Acceptance checklist**

* UI renders fields via partials.
* No Stimulus attributes remain in markup.

**Stop after Step 4. Output summary and diffs. Wait.**

---

## Step 5 — Vanilla JS modules

**Scope:** replace Stimulus with vanilla JS. No behavior changes.

**Files**

* `wwwroot/js/contentitems/attachment.js`
* `wwwroot/js/contentitems/wysiwyg.js`
* `wwwroot/js/contentitems/autocomplete.js`

**Implementation**

* `attachment.js`

  * Initialize Uppy on a container id pattern `${developerName}-uppy`.
  * Read config via `data-*` on the field wrapper: `data-pathbase`, `data-usedirectuploadtocloud`, `data-maxfilesize`, `data-mimetypes`.
  * On successful upload, set hidden input value to returned `objectKey`, show file info and remove button, support clear.
* `wysiwyg.js`

  * Initialize TipTap editor on a provided container.
  * Bind to hidden `<textarea>` for form submission. Sync on change.
  * Load `imagemediaitems` and `videomediaitems` JSON from `data-*`.
* `autocomplete.js`

  * On input, debounce, `fetch` to the existing `relationship/autocomplete` endpoint with `relatedContentTypeId` and `q`.
  * Render results as list items. On select, fill hidden value and primary field label. Handle add/remove actions.

**Markup hooks**

* Convert old `data-controller` attributes to simple `data-*` attributes you define. Keep them minimal and consistent.
* Reference scripts at the bottom of each page, only when the page needs them.

**Acceptance checklist**

* Create and Edit support attachment upload, TipTap editing, and relationship lookup with no Stimulus.
* No console errors.

**Stop after Step 5. Output summary and diffs. Wait.**

---

## Step 6 — Validation, choices, and state parity

**Scope:** ensure choices, multiple select, radio, dropdown, checkbox all keep the same selection and error behavior as legacy.

**Implementation**

* Maintain `pattern` and `asp-for` on numeric inputs.
* For `multiple_select`, ensure `AvailableChoices[j].Value` binds correctly to booleans and preserves selections after failed post.
* Render `invalid-feedback` exactly once per field and wire it to the right input.
* Confirm `Save as draft` and `Save and publish` both map to correct command values.

**Acceptance checklist**

* All legacy validation messages appear in the right place.
* Selection state persists after server side validation failures.

**Stop after Step 6. Output summary and diffs. Wait.**

---

## Step 7 — Authorization and readonly mode

**Scope:** enforce permissions and readonly view in Edit.

**Implementation**

* In `Edit.OnGetAsync`, evaluate `IAuthorizationService.AuthorizeAsync(User, contentTypeDeveloperName, ContentItemOperations.Edit)`.
* If not authorized, set a flag the page can use to render readonly components and the yellow alert.
* Ensure `_ActionsMenu` renders only when allowed.

**Acceptance checklist**

* Authorized users can edit. Unauthorized users see readonly UI and no posting behavior.
* No privilege escalation via client side controls.

**Stop after Step 7. Output summary and diffs. Wait.**

---

## Step 8 — Relationship autocomplete endpoint alignment

**Scope:** keep endpoint working, then optionally convert to a Razor Page handler.

**Phase A (keep working)**

* Leave existing MVC `RelationshipAutocomplete` action in place for now.
* Ensure `autocomplete.js` hits the same route and parses the `_Autocomplete` partial response.

**Phase B (optional migration)**

* Add Razor Page `Relationship.cshtml.cs` under `Pages/ContentItems/Relationship/Autocomplete` with `OnGet` returning the same `_Autocomplete` partial and model type `IEnumerable<KeyValuePair<string, string>>`.
* Update `autocomplete.js` to call the Razor Page route.
* Remove the legacy MVC action once parity is confirmed.

**Acceptance checklist**

* Typeahead works for Create/Edit, selects and clears values.
* No regressions when switching to Razor Page handler.

**Stop after Step 8. Output summary and diffs. Wait.**

---

### Deliverables per step

* Code changes scoped to that step only.
* A short summary and file-by-file diff.
* No additional refactors beyond the scope of the current step.

Execute **Step 1 only** now, then stop and wait for review.
