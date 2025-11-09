Here’s a drop-in Cursor prompt to refactor **BeginImportFromCsv** to Razor Pages, mirroring what we just did for **BeginExportToCsv** by folding BackgroundTaskStatus into the same page.

---

**Cursor Prompt: Migrate MVC → Razor Pages for BeginImportFromCsv and inline BackgroundTaskStatus**

Goal
Refactor the MVC “BeginImportFromCsv” flow to Razor Pages at `Pages/ContentTypes/Views/BeginImportFromCsv`. Keep the same URL shape and permissions. Start the background import job on POST, then stay on this page and display inline background-task status (same pattern we used for BeginExportToCsv after folding BackgroundTaskStatus into it). Use vanilla JS polling with a page-local handler.

Scope

* Create `Pages/ContentTypes/Views/BeginImportFromCsv.cshtml` and `BeginImportFromCsv.cshtml.cs`.
* Remove any dependency on a separate BackgroundTaskStatus page. Provide a JSON status handler inside this same Razor Page.
* Follow our established patterns from BeginExportToCsv refactor: PageModel attributes, TempData messaging, `_PageHeader`, `_BackToList`, minimal inline JS, no Stimulus/Hotwire.

Functional requirements

1. Routing, auth, filters

* Razor Pages route must match legacy URL shape:
  `@page "/{contentTypeDeveloperName}/views/{viewId}/import-from-csv"`
* Apply `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]`.
* Apply `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]` so `CurrentView` is available.

2. GET handler

* Set `ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > Import from CSV"`.
* Render `_PageHeader` with title and `Model.CurrentView.ContentTypeDescription`.
* Render `_BackToList` via `ContentItemsBackToList_ViewModel { ContentTypeDeveloperName = CurrentView.ContentTypeDeveloperName, IsEditing = false }`.
* If a `taskId` query param is present, render a slim status card/container and kick off polling to `?handler=Status&id={taskId}`.

3. Form and inputs

* Build the form with `method="post"` and `enctype="multipart/form-data"`.
* Do NOT use the legacy `asp-route="beginimportfromcsv"`; the Razor Page itself is the post target.
* Radio group bound to `ImportMethod` with values:

  * `add_new_records_only`
  * `upsert_all_records`
  * `update_existing_records_only`
    Default to `add_new_records_only`.
* File picker bound to `ImportFile` (`accept=".csv"`).
* Two submit buttons that post the same form but set `ImportAsDraft` to `true` or `false` respectively, matching legacy UX:

  * “Import as draft status” (secondary) → `name="ImportAsDraft" value="true"`
  * “Import as published status” (success) → `name="ImportAsDraft" value="false"`
* Show validation feedback for `ImportFile` like legacy (`HasError("ImportFile")`, `ErrorMessageFor("ImportFile")`) using our existing helpers or ModelState.

4. POST handler

* Read `IFormFile ImportFile`, stream to `byte[]` exactly like legacy.
* Send `BeginImportContentItemsFromCsv.Command` with:

  * `ImportMethod = ImportMethod`
  * `CsvAsBytes = <bytes>`
  * `ImportAsDraft = ImportAsDraft`
  * `ContentTypeId = CurrentView.ContentTypeId`
* On success:

  * `SetSuccessMessage("Import in progress.");`
  * Redirect to the SAME page with route values and the returned background task id as a query parameter, to avoid repost:
    `return RedirectToPage("/ContentTypes/Views/BeginImportFromCsv", new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id, taskId = response.Result });`
* On failure:

  * `SetErrorMessage("There was an error attempting while importing. See the error below.", response.GetErrors());`
  * Return `Page()` so validation and errors render.

5. Inline status (folded BackgroundTaskStatus)

* Add a JSON status handler on the same page:

  * `public async Task<IActionResult> OnGetStatusAsync(string id)`
  * Calls `Mediator.Send(new GetBackgroundTaskById.Query { Id = id })`
  * Returns `new JsonResult(response.Result)`
* In the page, if `taskId` exists, render a simple status card (“Import status”), and attach minimal JS to poll `?handler=Status&id=<taskId>` every N seconds. Stop polling when terminal state is reached (Succeeded, Failed, Canceled).
* Provide a “Back to list” link and optionally a soft “Refresh” button.

6. Anti-forgery & size limits

* Let the FormTagHelper emit the anti-forgery token.
* If our project uses upload limits, optionally add `[RequestSizeLimit(...)]` on the PageModel. Don’t block the refactor if unknown.

7. Code style & JS

* No Stimulus/Hotwire.
* Page-local `<script>` at bottom, namespaced to this page.
* Use `fetch` with `credentials: "same-origin"` for anti-forgery cookie context if needed. Keep it tiny and clear.

Suggested file content (signatures and key snippets)

`Pages/ContentTypes/Views/BeginImportFromCsv.cshtml`

* Top matter:

  ```csharp
  @page "/{contentTypeDeveloperName}/views/{viewId}/import-from-csv"
  @model BeginImportFromCsvModel
  @{
      ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > Import from CSV";
  }
  ```
* `_PageHeader` and `_BackToList` partials same as export page.
* Form:

  ```html
  <form method="post" enctype="multipart/form-data" class="py-4">
    <!-- ImportMethod radios -->
    <!-- ImportFile input + validation -->
    <hr />
    <button type="submit" class="btn btn-secondary" name="ImportAsDraft" value="true">Import as draft status</button>
    <button type="submit" class="btn btn-success mx-2" name="ImportAsDraft" value="false">Import as published status</button>
  </form>
  ```
* Status container shown only when `Model.TaskId != null` (or via query param). Minimal inline JS:

  ```html
  <div id="importStatus" data-task-id="@Model.TaskId" class="mt-3"></div>
  <script>
    (function(){
      const el = document.getElementById('importStatus');
      if(!el) return;
      const id = el.dataset.taskId;
      async function tick(){
        const res = await fetch(`?handler=Status&id=${encodeURIComponent(id)}`, { credentials: "same-origin" });
        if(!res.ok) return;
        const data = await res.json();
        // render simple status (state, progress, message)
        el.textContent = `Status: ${data?.state ?? 'unknown'}`;
        if (['Succeeded','Failed','Canceled'].includes((data?.state||'').toString())) return;
        setTimeout(tick, 3000);
      }
      tick();
    })();
  </script>
  ```

`Pages/ContentTypes/Views/BeginImportFromCsv.cshtml.cs`

```csharp
[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
public class BeginImportFromCsvModel : PageModelBase // use our base if present
{
    // Bind properties
    [BindProperty] public string ImportMethod { get; set; } = "add_new_records_only";
    [BindProperty] public IFormFile ImportFile { get; set; }
    [BindProperty] public bool ImportAsDraft { get; set; }

    // For inline status
    public string TaskId { get; set; }

    public IActionResult OnGet(string contentTypeDeveloperName, Guid viewId, string taskId = null)
    {
        TaskId = taskId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string contentTypeDeveloperName, Guid viewId)
    {
        byte[] fileBytes = null;
        if (ImportFile != null)
        {
            using var stream = ImportFile.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        var input = new BeginImportContentItemsFromCsv.Command
        {
            ImportMethod = ImportMethod,
            CsvAsBytes = fileBytes,
            ImportAsDraft = ImportAsDraft,
            ContentTypeId = CurrentView.ContentTypeId
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Import in progress.");
            return RedirectToPage("/ContentTypes/Views/BeginImportFromCsv",
                new { contentTypeDeveloperName = CurrentView.ContentType.DeveloperName, viewId = CurrentView.Id, taskId = response.Result });
        }

        SetErrorMessage("There was an error attempting while importing. See the error below.", response.GetErrors());
        return Page();
    }

    // Inline status endpoint
    public async Task<IActionResult> OnGetStatusAsync(string id)
    {
        var response = await Mediator.Send(new GetBackgroundTaskById.Query { Id = id });
        return new JsonResult(response.Result);
    }
}
```

Acceptance criteria

* GET `/{contentTypeDeveloperName}/views/{viewId}/import-from-csv` renders header, back link, and the import form.
* POST initiates the background import job, shows “Import in progress.”, and redirects back with `?taskId=...` to avoid resubmission.
* With `taskId` present, the page polls `?handler=Status&id=...` and displays live status until terminal.
* Authorization and `GetOrSetRecentlyAccessedViewFilterAttribute` applied.
* No Stimulus/Hotwire. Page-local, minimal JS only.
* Anti-forgery present. Form uses `multipart/form-data`.
* The legacy wrong `asp-route` is gone. Buttons set `ImportAsDraft` as expected.
