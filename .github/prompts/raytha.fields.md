## PROJECT PROMPT FOR CURSOR

**Goal**
Finish the Razor Pages migration for **ContentTypes/Fields/Create** and **ContentTypes/Fields/Edit**. Restore all “Choices” UI/UX that used to rely on Stimulus/Hotwire using **pure vanilla JavaScript**. No Stimulus, no Hotwire. Use **Reorder.cshtml** as the pattern for how we include page JS.

**Key points**

* Don’t implement “developername auto-slug” logic; it’s handled elsewhere. Provide a hook but leave it no-op.
* Avoid duplication between Create and Edit pages. Extract the Choices UI into a reusable partial and a single JS module.
* Preserve existing model binding names (`Choices[i].Label`, `Choices[i].DeveloperName`, `Choices[i].Disabled`).
* Toggle the entire Choices section when `FieldType` is one of `radio`, `dropdown`, `multiple_select`.
* Maintain Move Up / Move Down button visibility rules.
* Keep the checkbox value semantics identical to old code (`true` / `false` string set on change).
* Respect our existing pattern in **Reorder.cshtml** for script inclusion.

---

## Files to create / update

1. **Pages/ContentTypes/Fields/Create.cshtml**
2. **Pages/ContentTypes/Fields/Create.cshtml.cs**
3. **Pages/ContentTypes/Fields/Edit.cshtml**
4. **Pages/ContentTypes/Fields/Edit.cshtml.cs**
5. **Pages/Shared/EditorTemplates/_ChoicesEditor.cshtml**  ← reusable partial
6. **wwwroot/js/fieldChoices.js**  ← vanilla JS module

If our project keeps shared partials elsewhere, pick the right folder, but keep the name `_ChoicesEditor.cshtml`.

---

## Razor Pages: routing and handlers

Implement Razor Page routes that mirror the old MVC routes:

* Create:
  Route template: `/{contentTypeDeveloperName}/fields/create`
  Handlers: `OnGetAsync(string contentTypeDeveloperName)` and `OnPostAsync(string contentTypeDeveloperName)`

* Edit:
  Route template: `/{contentTypeDeveloperName}/fields/edit/{id}`
  Handlers: `OnGetAsync(string contentTypeDeveloperName, string id)` and `OnPostAsync(string contentTypeDeveloperName, string id)`

Inject whatever you need (e.g., `IMediator`, `CurrentView` accessor/filter equivalent). Preserve the same mediator calls and success/error paths shown in the old MVC actions.

On validation failure, **rebuild** `AvailableContentTypes` and `AvailableFieldTypes` so the page re-renders correctly, same as MVC.

---

## Shared Choices partial (no duplication)

**Pages/Shared/EditorTemplates/_ChoicesEditor.cshtml**

* Inputs:

  * `Model.Choices` (array or empty)
  * `Model.FieldType`
  * The partial must render:

    * Wrapper with `id="choicesList"` that contains rows
    * “Add another choice” button
    * Correct Bootstrap classes and the same markup structure expected by the JS
  * The partial must not include Stimulus/Hotwire attributes. Replace them with `data-` attributes that our vanilla JS module will read.

Use this HTML structure for each row so the JS can target it:

```cshtml
@model dynamic
@{
    // Expect the caller's model to expose:
    //   IEnumerable<FieldChoiceItem_ViewModel> Choices
    //   string FieldType
    //   string FieldChoicesToggleTargetId  // unique id for the collapsible/hidden section container
    // Render the section hidden/shown by FieldType; JS will also toggle it.
    var index = 0;
}

<div class="col-lg-12" id="@Model.FieldChoicesToggleTargetId">
    <div class="mb-3">
        <label class="form-label raytha-required">Choices</label>
        <div id="choicesList" class="choices-list">
            @if (Model.Choices != null && Model.Choices.Any())
            {
                foreach (var c in Model.Choices)
                {
                    <div class="row form-row" data-choice-row>
                        <div class="col-4">
                            <div class="mb-3">
                                <input type="text"
                                       name="Choices[@index].Label"
                                       class="form-control form-row-field"
                                       value="@c.Label"
                                       data-choice-label
                                       data-ordinal="@index"
                                       required>
                            </div>
                        </div>
                        <div class="col-4">
                            <div class="mb-3">
                                <input type="text"
                                       name="Choices[@index].DeveloperName"
                                       class="form-control form-row-field"
                                       value="@c.DeveloperName"
                                       data-choice-developername
                                       required>
                            </div>
                        </div>
                        <div class="col-3">
                            <div class="mb-3">
                                <div class="form-check">
                                    <input class="form-check-input form-row-field"
                                           type="checkbox"
                                           name="Choices[@index].Disabled"
                                           value="@(c.Disabled ? "true" : "false")"
                                           @(c.Disabled ? "checked" : "")
                                           data-choice-disabled>
                                    <label class="form-check-label">Disabled</label>
                                </div>
                            </div>
                        </div>
                        <div class="col-1">
                            <div class="dropdown">
                                <a class="dropdown-toggle" data-bs-toggle="dropdown" data-bs-auto-close="true" href="#" role="button" aria-expanded="false">
                                    <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h.01M12 12h.01M19 12h.01"/></svg>
                                </a>
                                <ul class="dropdown-menu">
                                    <li><a class="dropdown-item" href="javascript:void(0);" data-action="insert">Insert</a></li>
                                    <li><a class="dropdown-item" href="javascript:void(0);" data-action="moveUp" data-move-up>Move up</a></li>
                                    <li><a class="dropdown-item" href="javascript:void(0);" data-action="moveDown" data-move-down>Move down</a></li>
                                    <li><hr class="dropdown-divider"></li>
                                    <li><a class="dropdown-item link-danger" href="javascript:void(0);" data-action="remove">Remove</a></li>
                                </ul>
                            </div>
                        </div>
                    </div>
                    index++;
                }
            }
        </div>

        <div class="row">
            <div class="col-12">
                <a href="javascript:void(0);" class="btn btn-sm btn-secondary" data-action="addAnother">Add another choice</a>
            </div>
        </div>
    </div>
</div>
```

The page that calls this partial must pass a **stable unique id** into `FieldChoicesToggleTargetId`, e.g. `"choicesSection"`.

---

## Vanilla JS module

**wwwroot/js/fieldChoices.js**

Implement a class that binds to:

* a `<select>` for FieldType
* the **choices section container** by id (e.g., `choicesSection`)
* the `<div id="choicesList">` container
* the add/insert/move up/move down/remove actions
* maintains correct name indices for `Choices[i].*`
* sets checkbox value to `"true"`/`"false"` on change
* no Stimulus, no external libs

Use this exact public API so multiple pages can reuse it:

```js
// wwwroot/js/fieldChoices.js
export class FieldChoicesController {
  constructor({ fieldTypeSelect, choicesSection, choicesList, addAnotherButton, enableAutoSlug = false, onSlugify = null }) {
    this.fieldTypeSelect = fieldTypeSelect;     // <select>
    this.choicesSection = choicesSection;       // section container
    this.choicesList = choicesList;             // <div id="choicesList">
    this.addAnotherButton = addAnotherButton;   // <a data-action="addAnother">
    this.enableAutoSlug = enableAutoSlug;       // keep false; slug handled elsewhere
    this.onSlugify = onSlugify;                 // optional hook

    this.formRowClass = "form-row";
    this.formRowFieldClass = "form-row-field";

    this.bind();
    this.toggleChoicesSection();
    this.updateMoveButtons();
  }

  bind() {
    if (this.fieldTypeSelect) {
      this.fieldTypeSelect.addEventListener("change", () => {
        this.toggleChoicesSection();
        this.updateMoveButtons();
      });
    }

    if (this.addAnotherButton) {
      this.addAnotherButton.addEventListener("click", (e) => {
        e.preventDefault();
        this.addRowAtEnd();
      });
    }

    // Delegate row actions
    this.choicesList.addEventListener("click", (e) => {
      const action = e.target.closest("[data-action]")?.getAttribute("data-action");
      if (!action) return;

      e.preventDefault();
      const row = e.target.closest("[data-choice-row]");

      switch (action) {
        case "insert":
          this.insertRowBefore(row);
          break;
        case "moveUp":
          this.moveUp(row);
          break;
        case "moveDown":
          this.moveDown(row);
          break;
        case "remove":
          this.removeRow(row);
          break;
      }
    });

    // Delegate checkbox true/false behavior
    this.choicesList.addEventListener("change", (e) => {
      if (e.target.matches("[data-choice-disabled]")) {
        e.target.value = e.target.checked ? "true" : "false";
      }
    });

    // Optional: label -> slug hook
    this.choicesList.addEventListener("input", (e) => {
      if (!this.enableAutoSlug || !this.onSlugify) return;
      if (e.target.matches("[data-choice-label]")) {
        const row = e.target.closest("[data-choice-row]");
        const dn = row.querySelector("[data-choice-developername]");
        if (dn) dn.value = this.onSlugify(e.target.value);
      }
    });
  }

  toggleChoicesSection() {
    if (!this.fieldTypeSelect || !this.choicesSection) return;
    const val = this.fieldTypeSelect.value;
    const visible = val === "radio" || val === "dropdown" || val === "multiple_select";
    this.choicesSection.hidden = !visible;
  }

  addRowAtEnd() {
    const newRow = this.buildRow();
    this.choicesList.appendChild(newRow);
    this.reindex();
    this.updateMoveButtons();
  }

  insertRowBefore(row) {
    const newRow = this.buildRow();
    this.choicesList.insertBefore(newRow, row);
    this.reindex();
    this.updateMoveButtons();
  }

  moveUp(row) {
    const prev = row.previousElementSibling;
    if (prev) {
      this.choicesList.insertBefore(row, prev);
      this.reindex();
      this.updateMoveButtons();
    }
  }

  moveDown(row) {
    const next = row.nextElementSibling;
    if (next) {
      this.choicesList.insertBefore(next, row);
      this.reindex();
      this.updateMoveButtons();
    }
  }

  removeRow(row) {
    this.choicesList.removeChild(row);
    this.reindex();
    this.updateMoveButtons();
  }

  reindex() {
    const rows = [...this.choicesList.querySelectorAll(`[data-choice-row]`)];
    rows.forEach((row, i) => {
      const fields = row.querySelectorAll(`.${this.formRowFieldClass}, [data-choice-label], [data-choice-developername], [data-choice-disabled]`);
      fields.forEach((el) => {
        // update name="Choices[x].Something"
        if (el.name) {
          el.name = el.name.replace(/\[([^\]]+)]/g, `[${i}]`);
        }
        // update ordinal attribute
        if (el.hasAttribute("data-ordinal")) {
          el.setAttribute("data-ordinal", String(i));
        }
      });
    });
  }

  updateMoveButtons() {
    const rows = [...this.choicesList.querySelectorAll(`[data-choice-row]`)];
    rows.forEach((row, i) => {
      const up = row.querySelector("[data-move-up]")?.closest("li") || null;
      const down = row.querySelector("[data-move-down]")?.closest("li") || null;
      if (up) up.hidden = i === 0;
      if (down) down.hidden = i === rows.length - 1;
    });
  }

  buildRow() {
    const wrapper = document.createElement("div");
    wrapper.className = `row ${this.formRowClass}`;
    wrapper.setAttribute("data-choice-row", "");

    wrapper.innerHTML = `
      <div class="col-4">
        <div class="mb-3">
          <input type="text" name="Choices[0].Label" class="form-control ${this.formRowFieldClass}" data-choice-label data-ordinal="0" required>
        </div>
      </div>
      <div class="col-4">
        <div class="mb-3">
          <input type="text" name="Choices[0].DeveloperName" class="form-control ${this.formRowFieldClass}" data-choice-developername required>
        </div>
      </div>
      <div class="col-3">
        <div class="mb-3">
          <div class="form-check">
            <input class="form-check-input ${this.formRowFieldClass}" type="checkbox" value="false" name="Choices[0].Disabled" data-choice-disabled>
            <label class="form-check-label">Disabled</label>
          </div>
        </div>
      </div>
      <div class="col-1">
        <div class="dropdown">
          <a class="dropdown-toggle" data-bs-toggle="dropdown" data-bs-auto-close="true" href="#" role="button" aria-expanded="false">
            <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h.01M12 12h.01M19 12h.01"/></svg>
          </a>
          <ul class="dropdown-menu">
            <li><a class="dropdown-item" href="javascript:void(0);" data-action="insert">Insert</a></li>
            <li><a class="dropdown-item" href="javascript:void(0);" data-action="moveUp" data-move-up>Move up</a></li>
            <li><a class="dropdown-item" href="javascript:void(0);" data-action="moveDown" data-move-down>Move down</a></li>
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item link-danger" href="javascript:void(0);" data-action="remove">Remove</a></li>
          </ul>
        </div>
      </div>
    `;
    return wrapper;
  }
}
```

---

## Wire up per page

In both **Create.cshtml** and **Edit.cshtml**:

* Render the shared partial with a stable section id (e.g., `choicesSection`).
* Give the FieldType `<select>` an `id` we can target, like `id="fieldType"`.
* After the form markup, include the page-level initialization script using the **same inclusion technique used in Reorder.cshtml**. Cursor should open `Reorder.cshtml` and mirror that pattern exactly. If `Reorder.cshtml` uses `<script type="module" src="/js/whatever.js"></script>`, do the same here.

Example snippet near the bottom of the page (adjust to match Reorder.cshtml’s pattern):

```cshtml
@section Scripts {
    <script type="module">
        import { FieldChoicesController } from '/js/fieldChoices.js';

        const fieldTypeSelect = document.getElementById('fieldType');
        const choicesSection  = document.getElementById('choicesSection');
        const choicesList     = document.getElementById('choicesList');
        const addAnotherBtn   = document.querySelector('[data-action="addAnother"]');

        // developername slugging is handled elsewhere; keep hook disabled.
        const controller = new FieldChoicesController({
            fieldTypeSelect,
            choicesSection,
            choicesList,
            addAnotherButton: addAnotherBtn,
            enableAutoSlug: false,
            onSlugify: null
        });
    </script>
}
```

---

## Create.cshtml(.cs) specifics

* Mirror MVC GET to populate:

  * `ContentTypeId = CurrentView.ContentTypeId`
  * `AvailableContentTypes` and `AvailableFieldTypes`
* POST maps to `CreateContentTypeField.Command` with the same field mappings as MVC.
* On success: show success message and redirect to FieldsList (same route logic).
* On failure: show error and repopulate selects before returning `Page()`.

Pass to `_ChoicesEditor.cshtml`:

* `Choices = Model.Choices ?? new FieldChoiceItem_ViewModel[0]`
* `FieldType = Model.FieldType`
* `FieldChoicesToggleTargetId = "choicesSection"`

Make sure the FieldType `<select>` has `id="fieldType"`.

---

## Edit.cshtml(.cs) specifics

* GET loads data via `GetContentTypeFieldById.Query` and `GetContentTypes.Query` just like MVC and maps into the edit view model.
* Keep FieldType and RelatedContentType disabled in the UI, as in MVC.
* POST maps to `EditContentTypeField.Command` with the same fields as MVC.
* Success/failure behavior identical to MVC.

Same partial and same JS wire-up as Create.

---

## Acceptance criteria

* Choices section shows only for FieldType ∈ {radio, dropdown, multiple_select}. Hidden otherwise. Toggling works on change.
* Add Another inserts a new row with correct name indexes. Insert, Move Up, Move Down, Remove all work and keep form names correct for model binding.
* Disabled checkbox sets value to `"true"` when checked and `"false"` when unchecked.
* First row has Move Up hidden, last row has Move Down hidden. This updates correctly after any operation.
* No Stimulus or Hotwire references anywhere.
* JS is loaded and initialized using the **exact** inclusion style found in **Reorder.cshtml**.
* Create and Edit pages share the same partial and same JS module with zero copy/paste duplication besides page-specific form fields.
* Server-side validation errors repopulate dropdowns and re-render existing choices without losing user input.

---

## Notes for Cursor

* Search for `Reorder.cshtml` and replicate its script-inclusion pattern exactly.
* Keep TypeScript out of scope unless the project already uses it in wwwroot. Use vanilla ES modules.
* Match existing Bootstrap classes and icon svgs already used in the old view.
* Keep the `asp-` tag helpers and current route names intact.
* Don’t touch the separate “developername” auto-slug feature; leave only a hook as shown.

---

End of prompt.
