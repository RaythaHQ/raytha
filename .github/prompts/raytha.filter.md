You did a great job with porting over Sort and Columns. Look at my responses and guidance for your Sort task and keep consistent here. This will be more challenging than the others. Optimize any javascript / follow javascript best practices if if the existing code is poorly written.
---

**Goal**
Port legacy MVC `ContentTypes/Views/Filter` to a Razor Page. Preserve URL shape, auth, filters, UX, and the tree-edit behavior. Replace the Stimulus controller with a small vanilla ES module that mirrors the same data attributes so the markup barely changes. Follow the same structure you used for `Sort`, `Columns`, and `PublicSettings`, and align with `ContentTypes/Views/Edit`.

**Key requirements**

* Keep route: `/raytha/{contentTypeDeveloperName}/views/{viewId}/filter`
* Handlers:

  * `OnGetAsync` to load field metadata and current filter tree
  * `OnPostSaveAsync` to accept JSON and persist via `EditFilter.Command`
* Authorization/filter attributes:

  * `[ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]`
  * `[Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]`
* Security: include antiforgery token in the fetch request (no `[IgnoreAntiforgeryToken]` here)
* Reuse existing partial `_FilterSubtree` with minimal changes, scoped in same folder
* JS: vanilla ES module `filter.js` placed in our admin assets and imported on this page only; no Stimulus

**Create these files**

`Areas/Admin/Pages/ContentTypes/Views/Filter.cshtml`

```cshtml
@page "{contentTypeDeveloperName}/views/{viewId:guid}/filter/{handler?}"
@model Areas.Admin.Pages.ContentTypes.Views.FilterModel
@using System.Text.Json
@{
    ViewData["Title"] = $"{Model.CurrentView.ContentTypeLabelPlural} > {Model.CurrentView.Label} > Filter";
    var initialParentId = Guid.NewGuid().ToString();
}
@section headstyles {
  <link rel="stylesheet" href="~/raytha_admin/css/notyf.min.css" />
  <style>
    .tree { min-height:20px; }
    .tree li { list-style-type:none; margin:0; padding:10px 5px 0 5px; position:relative }
    .tree li::before, .tree li::after { content:''; left:-20px; position:absolute; right:auto }
    .tree li::before { border-left:1px solid #999; bottom:50px; height:100%; top:0; width:1px }
    .tree li::after { border-top:1px solid #999; height:20px; top:25px; width:25px }
    .tree > ul > li::before, .tree > ul > li::after { border:0 }
    .tree li:last-child::before { height:30px }
  </style>
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

        <!-- Antiforgery token host; JS will read it and send in a header -->
        <form id="filter-af-token-host">@Html.AntiForgeryToken()</form>

        <div id="filter-root"
             class="tree"
             data-customfields='@Model.ContentTypeFieldsJson'
             data-updateurl="@Url.Page("Filter", "Save", new { contentTypeDeveloperName = Model.CurrentView.ContentTypeDeveloperName, viewId = Model.CurrentView.Id })">

          @if (Model.HasExistingFilter)
          {
              <ul>
                @await Html.PartialAsync("_FilterSubtree", new FilterSubtree_ViewModel {
                    ContentTypeFields = Model.ContentTypeFields,
                    FilterCondition = Model.RootFilterCondition,
                    CurrentView = Model.CurrentView
                })
              </ul>
          }
          else
          {
              <ul>
                <li class="parent_li"
                    data-type="filter_condition_group"
                    data-id="@initialParentId"
                    data-parentid="">
                  <div class="card">
                    <div class="card-body">
                      <div class="form-check form-check-inline">
                        <input class="form-check-input" name="groupOperator-@initialParentId" id="andChoice-@initialParentId" type="radio" value="and" data-forid="@initialParentId" checked>
                        <label class="form-check-label" for="andChoice-@initialParentId">AND</label>
                      </div>
                      <div class="form-check form-check-inline">
                        <input class="form-check-input" name="groupOperator-@initialParentId" id="orChoice-@initialParentId" type="radio" value="or" data-forid="@initialParentId">
                        <label class="form-check-label" for="orChoice-@initialParentId">OR</label>
                      </div>
                      <div class="float-end">
                        <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter" data-forid="@initialParentId">Add filter</button>
                        <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter-group" data-forid="@initialParentId">Add filter group</button>
                      </div>
                      <ul data-forid="@initialParentId"></ul>
                    </div>
                  </div>
                </li>
              </ul>
          }

          <button type="button" id="filter-save" class="btn btn-success">Save</button>
        </div>

      </div>
    </div>
  </div>
</div>

@section scripts {
  <script type="module" src="~/raytha_admin/js/filter.js"></script>
}
```

`Areas/Admin/Pages/ContentTypes/Views/_FilterSubtree.cshtml`
(same markup as legacy partial, but drop Stimulus-specific `data-*-target` attributes; keep semantic data attributes we actually use in vanilla JS)

```cshtml
@using Raytha.Domain.ValueObjects.FieldTypes
@model FilterSubtree_ViewModel
@removeTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}
@if (Model != null)
{
<li class="parent_li"
    data-type="@Model.FilterCondition.Type.DeveloperName"
    data-id="@Model.FilterCondition.Id"
    data-parentid="@Model.FilterCondition.ParentId">
  <div class="card">
    <div class="card-body">

      @if (Model.FilterCondition.Type.DeveloperName == Domain.ValueObjects.FilterConditionType.FilterConditionGroup)
      {
        <div class="form-check form-check-inline">
          <input class="form-check-input" name="groupOperator-@Model.FilterCondition.Id" id="andChoice-@Model.FilterCondition.Id" type="radio" value="and" data-forid="@Model.FilterCondition.Id" @(Model.FilterCondition.GroupOperator.DeveloperName == Domain.ValueObjects.BooleanOperator.AND ? "checked" : "")>
          <label class="form-check-label" for="andChoice-@Model.FilterCondition.Id">AND</label>
        </div>
        <div class="form-check form-check-inline">
          <input class="form-check-input" name="groupOperator-@Model.FilterCondition.Id" id="orChoice-@Model.FilterCondition.Id" type="radio" value="or" data-forid="@Model.FilterCondition.Id" @(Model.FilterCondition.GroupOperator.DeveloperName == Domain.ValueObjects.BooleanOperator.OR ? "checked" : "")>
          <label class="form-check-label" for="orChoice-@Model.FilterCondition.Id">OR</label>
        </div>
        <div class="float-end">
          <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter" data-forid="@Model.FilterCondition.Id">Add filter</button>
          <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter-group" data-forid="@Model.FilterCondition.Id">Add filter group</button>
          @if (Model.FilterCondition.ParentId.HasValue)
          {
            <button type="button" class="btn btn-sm btn-outline-danger" data-action="remove" data-forid="@Model.FilterCondition.Id">
              <svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
            </button>
          }
        </div>
      }
      else
      {
        var field = Model.ContentTypeFields.First(p => p.DeveloperName == Model.FilterCondition.Field);

        <form class="row gy-2 gx-3 align-items-center">
          <div class="col-md-3">
            <label class="visually-hidden" for="field">Field</label>
            <select class="form-select" id="field-@Model.FilterCondition.Id" data-forid="@Model.FilterCondition.Id" data-role="choose-field">
              <option value="">Choose field...</option>
              @foreach (var option in Model.ContentTypeFields)
              {
                <option value="@option.DeveloperName" @(Model.FilterCondition.Field == option.DeveloperName ? "selected" : "")>@option.DeveloperName</option>
              }
            </select>
          </div>
          <div class="col-md-3">
            <label class="visually-hidden" for="operator">Operator</label>
            <select class="form-select" id="operator-@Model.FilterCondition.Id" data-forid="@Model.FilterCondition.Id" data-role="choose-operator">
              <option value="">Choose operator...</option>
              @foreach (var option in field.FieldType.SupportedConditionOperators)
              {
                <option value="@option.DeveloperName" @(Model.FilterCondition.ConditionOperator.DeveloperName == option.DeveloperName ? "selected" : "")>@option.Label</option>
              }
            </select>
          </div>
          <div class="col-md-5">
            <label class="visually-hidden">Value</label>
            <div data-role="filter-value-container" data-forid="@Model.FilterCondition.Id">
              @if (Model.FilterCondition.ConditionOperator.HasFieldValue)
              {
                if (field.FieldType.HasChoices)
                {
                  <select class="form-select" id="filter-value-@Model.FilterCondition.Id" data-forid="@Model.FilterCondition.Id" data-role="filter-value">
                    <option value="" selected>Choose value...</option>
                    @foreach (var choice in field.Choices)
                    {
                      <option value="@choice" @(choice == Model.FilterCondition.Value ? "selected": "")>@choice</option>
                    }
                  </select>
                }
                else if (field.FieldType.DeveloperName == BaseFieldType.Date)
                {
                  <input type="date" class="form-control" data-role="filter-value" data-forid="@Model.FilterCondition.Id" id="filter-value-@Model.FilterCondition.Id" value="@Model.FilterCondition.Value">
                }
                else if (field.FieldType.DeveloperName == BaseFieldType.Number)
                {
                  <input type="number" class="form-control" data-role="filter-value" data-forid="@Model.FilterCondition.Id" id="filter-value-@Model.FilterCondition.Id" value="@Model.FilterCondition.Value">
                }
                else
                {
                  <input type="text" class="form-control" data-role="filter-value" data-forid="@Model.FilterCondition.Id" id="filter-value-@Model.FilterCondition.Id" value="@Model.FilterCondition.Value">
                }
              }
            </div>
          </div>
          <div class="col-md-1 ms-auto">
            <button data-forid="@Model.FilterCondition.Id" data-action="remove" type="button" class="btn btn-sm btn-outline-danger">
              <svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
            </button>
          </div>
        </form>
      }

      <ul data-forid="@Model.FilterCondition.Id">
        @foreach (var child in Model.CurrentView.Filter.Where(p => p.ParentId == Model.FilterCondition.Id))
        {
          @await Html.PartialAsync("_FilterSubtree", new FilterSubtree_ViewModel {
              FilterCondition = child,
              ContentTypeFields = Model.ContentTypeFields,
              CurrentView = Model.CurrentView
          })
        }
      </ul>
    </div>
  </div>
</li>
}
```

`Areas/Admin/Pages/ContentTypes/Views/Filter.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raytha.Application.ContentTypes.Queries;
using Raytha.Application.Views.Commands;
using Raytha.Application.Views.Queries;
using Raytha.Domain.ValueObjects;
using System.Text.Json;

namespace Areas.Admin.Pages.ContentTypes.Views
{
    [ServiceFilter(typeof(GetOrSetRecentlyAccessedViewFilterAttribute))]
    [Authorize(Policy = BuiltInContentTypePermission.CONTENT_TYPE_EDIT_PERMISSION)]
    public class FilterModel : RaythaPageModel
    {
        private readonly IMediator _mediator;

        public FilterModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        [BindProperty(SupportsGet = true)]
        public string ContentTypeDeveloperName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ViewId { get; set; }

        // For rendering
        public ViewsFilterContentTypeField_ViewModel[] ContentTypeFields { get; set; } = Array.Empty<ViewsFilterContentTypeField_ViewModel>();
        public string ContentTypeFieldsJson { get; set; } = "[]";

        public bool HasExistingFilter => CurrentView.Filter != null && CurrentView.Filter.Any();
        public dynamic RootFilterCondition => CurrentView.Filter?.FirstOrDefault(p => p.ParentId == null);

        public async Task<IActionResult> OnGetAsync()
        {
            var response = await _mediator.Send(new GetContentTypeFields.Query
            {
                PageSize = int.MaxValue,
                OrderBy = $"FieldOrder {SortOrder.ASCENDING}",
                DeveloperName = CurrentView.ContentType.DeveloperName
            });

            var contentTypeFields = response.Result.Items.Select(p => new ViewsFilterContentTypeField_ViewModel
            {
                Label = p.Label,
                DeveloperName = p.DeveloperName,
                FieldType = p.FieldType,
                Choices = p.Choices?.Select(x => x.DeveloperName)
            }).ToList();

            var builtIns = BuiltInContentTypeField.ReservedContentTypeFields
                .Where(p => p.DeveloperName != BuiltInContentTypeField.CreatorUser && p.DeveloperName != BuiltInContentTypeField.LastModifierUser)
                .Select(p => new ViewsFilterContentTypeField_ViewModel
                {
                    Label = p.Label,
                    DeveloperName = p.DeveloperName,
                    FieldType = p.FieldType
                }).ToList();

            contentTypeFields.AddRange(builtIns);

            ContentTypeFields = contentTypeFields.ToArray();
            ContentTypeFieldsJson = JsonSerializer.Serialize(ContentTypeFields);

            return Page();
        }

        // Accepts JSON "json" via multipart/form-data from fetch()
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveAsync([FromForm] string json)
        {
            IEnumerable<FilterConditionInputDto> items;
            try
            {
                items = JsonSerializer.Deserialize<IEnumerable<FilterConditionInputDto>>(json) ?? Enumerable.Empty<FilterConditionInputDto>();
            }
            catch
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new JsonResult(new { success = false, error = "Invalid JSON." });
            }

            var response = await _mediator.Send(new EditFilter.Command
            {
                Id = CurrentView.Id,
                Filter = items
            });

            if (!response.Success)
                Response.StatusCode = StatusCodes.Status400BadRequest;

            return new JsonResult(response);
        }
    }
}
```

`wwwroot/raytha_admin/js/filter.js`
(vanilla ES module replacing Stimulus; mirrors data attributes)

```javascript
import { v4 as uuidv4 } from "uuid";
import { Notyf } from "notyf";

(function () {
  const root = document.getElementById("filter-root");
  if (!root) return;

  const updateUrl = root.getAttribute("data-updateurl");
  const customFields = safeParseJSON(root.getAttribute("data-customfields")) || [];
  const devNames = customFields.map(o => o.DeveloperName);
  const notyf = new Notyf();

  // Events
  root.addEventListener("click", (e) => {
    const el = e.target.closest("[data-action]");
    if (!el) return;
    const action = el.getAttribute("data-action");
    const id = el.getAttribute("data-forid");
    if (action === "add-filter") addFilter(id);
    if (action === "add-filter-group") addFilterGroup(id);
    if (action === "remove") removeNode(id);
  });

  root.addEventListener("change", (e) => {
    const fieldSel = e.target.closest("[data-role='choose-field']");
    const opSel = e.target.closest("[data-role='choose-operator']");
    if (fieldSel) onChooseField(fieldSel);
    if (opSel) onChooseOperator(opSel);
  });

  document.getElementById("filter-save").addEventListener("click", onSave);

  // Helpers
  function subtreeFor(id) {
    return root.querySelector(`ul[data-forid="${cssEscape(id)}"]`);
  }
  function rowFor(id) {
    return root.querySelector(`.parent_li[data-id="${cssEscape(id)}"]`);
  }

  function addFilter(parentId) {
    const id = uuidv4();
    const li = document.createElement("li");
    li.className = "parent_li";
    li.setAttribute("data-id", id);
    li.setAttribute("data-type", "filter_condition");
    li.setAttribute("data-parentid", parentId);
    li.innerHTML = filterTemplate(id);
    (subtreeFor(parentId) || rowFor(parentId).querySelector("ul[data-forid]")).appendChild(li);
  }

  function addFilterGroup(parentId) {
    const id = uuidv4();
    const li = document.createElement("li");
    li.className = "parent_li";
    li.setAttribute("data-id", id);
    li.setAttribute("data-type", "filter_condition_group");
    li.setAttribute("data-parentid", parentId);
    li.innerHTML = groupTemplate(id);
    (subtreeFor(parentId) || rowFor(parentId).querySelector("ul[data-forid]")).appendChild(li);
  }

  function removeNode(id) {
    const el = rowFor(id);
    if (el) el.remove();
  }

  function onChooseField(selectEl) {
    const id = selectEl.getAttribute("data-forid");
    const field = customFields.find(x => x.DeveloperName === selectEl.value);
    const opSelect = root.querySelector(`#operator-${cssEscape(id)}`);
    const valueContainer = root.querySelector(`[data-role="filter-value-container"][data-forid="${cssEscape(id)}"]`);
    if (!opSelect || !valueContainer) return;

    if (field) {
      opSelect.innerHTML = operatorsForFieldType(field.FieldType);
      opSelect.parentElement.hidden = false;
      valueContainer.parentElement.hidden = true;
      valueContainer.innerHTML = ""; // reset
    } else {
      opSelect.parentElement.hidden = true;
      valueContainer.parentElement.hidden = true;
      valueContainer.innerHTML = "";
    }
  }

  function onChooseOperator(opSelect) {
    const id = opSelect.getAttribute("data-forid");
    const fieldSel = root.querySelector(`#field-${cssEscape(id)}`);
    const field = customFields.find(x => x.DeveloperName === (fieldSel ? fieldSel.value : ""));
    const valueContainer = root.querySelector(`[data-role="filter-value-container"][data-forid="${cssEscape(id)}"]`);
    if (!valueContainer || !field) return;

    const op = opSelect.value;
    valueContainer.parentElement.hidden = false;
    valueContainer.innerHTML = htmlFieldFor(op, id, field);
  }

  async function onSave(e) {
    e.preventDefault();
    const items = [];
    root.querySelectorAll(".parent_li").forEach((el) => {
      const id = el.getAttribute("data-id");
      const parentId = el.getAttribute("data-parentid");
      const type = el.getAttribute("data-type");

      const item = {
        Id: id,
        ParentId: parentId === "" ? null : parentId,
        Type: type,
        GroupOperator: "",
        Field: "",
        ConditionOperator: "",
        Value: ""
      };

      if (type === "filter_condition_group") {
        const andRadio = root.querySelector(`input[name="groupOperator-${cssEscape(id)}"][value="and"]`);
        const isAnd = andRadio && andRadio.checked;
        item.GroupOperator = isAnd ? "and" : "or";
      } else {
        const fieldSel = root.querySelector(`#field-${cssEscape(id)}`);
        const opSel = root.querySelector(`#operator-${cssEscape(id)}`);
        const valEl = root.querySelector(`#filter-value-${cssEscape(id)}`);
        item.Field = fieldSel ? fieldSel.value : "";
        item.ConditionOperator = opSel ? opSel.value : "";
        if (valEl) item.Value = valEl.value;
      }

      items.push(item);
    });

    const token = getAntiForgeryToken();
    const fd = new FormData();
    fd.append("json", JSON.stringify(items));

    try {
      const res = await fetch(updateUrl, {
        method: "POST",
        headers: token ? { "RequestVerificationToken": token } : {},
        body: fd
      });
      const json = await res.json();
      if (res.ok && json && json.success) {
        notyf.success("Filter successfully updated");
      } else {
        notyf.error(json && json.error ? json.error : "Error saving filter");
      }
    } catch {
      notyf.error("Network error saving filter");
    }
  }

  // Templating helpers
  function defaultChooseFieldOptions() {
    let html = `<option value="" selected>Choose field...</option>`;
    devNames.forEach(n => { html += `<option value="${escapeHtml(n)}">${escapeHtml(n)}</option>`; });
    return html;
  }

  function operatorsForFieldType(fieldType) {
    let html = `<option value="" selected>Choose operator...</option>`;
    (fieldType?.SupportedConditionOperators || []).forEach(op => {
      html += `<option value="${escapeHtml(op.DeveloperName)}">${escapeHtml(op.Label)}</option>`;
    });
    return html;
  }

  function htmlFieldFor(operator, id, customField) {
    const none = ["empty","notempty","","null","true","false"];
    if (none.includes(operator)) return "";

    if (customField.FieldType?.HasChoices) {
      const choices = (customField.Choices || []).map(o => `<option value="${escapeHtml(o)}">${escapeHtml(o)}</option>`).join("");
      return `<select class="form-select" id="filter-value-${id}" data-forid="${id}" data-role="filter-value">
                <option value="" selected>Choose value...</option>${choices}
              </select>`;
    } else if (customField.FieldType?.DeveloperName === "date") {
      return `<input type="date" class="form-control" data-role="filter-value" data-forid="${id}" id="filter-value-${id}">`;
    } else if (customField.FieldType?.DeveloperName === "number") {
      return `<input type="number" class="form-control" data-role="filter-value" data-forid="${id}" id="filter-value-${id}" placeholder="0">`;
    } else {
      return `<input type="text" class="form-control" data-role="filter-value" data-forid="${id}" id="filter-value-${id}" placeholder="Value">`;
    }
  }

  function filterTemplate(id) {
    return `
      <div class="card"><div class="card-body">
        <form class="row gy-2 gx-3 align-items-center">
          <div class="col-md-3">
            <label class="visually-hidden" for="field">Field</label>
            <select class="form-select" id="field-${id}" data-forid="${id}" data-role="choose-field">
              ${defaultChooseFieldOptions()}
            </select>
          </div>
          <div class="col-md-3" >
            <label class="visually-hidden" for="operator">Operator</label>
            <select class="form-select" id="operator-${id}" data-forid="${id}" data-role="choose-operator"></select>
          </div>
          <div class="col-md-5" >
            <label class="visually-hidden">Value</label>
            <div data-role="filter-value-container" data-forid="${id}"></div>
          </div>
          <div class="col-md-1 ms-auto">
            <button data-forid="${id}" data-action="remove" type="button" class="btn btn-sm btn-outline-danger">
              <svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
            </button>
          </div>
        </form>
      </div></div>`;
  }

  function groupTemplate(id) {
    return `
      <div class="card"><div class="card-body">
        <div class="form-check form-check-inline">
          <input class="form-check-input" name="groupOperator-${id}" id="andChoice-${id}" type="radio" value="and" data-forid="${id}" checked>
          <label class="form-check-label" for="andChoice-${id}">AND</label>
        </div>
        <div class="form-check form-check-inline">
          <input class="form-check-input" name="groupOperator-${id}" id="orChoice-${id}" type="radio" value="or" data-forid="${id}">
          <label class="form-check-label" for="orChoice-${id}">OR</label>
        </div>
        <div class="float-end">
          <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter" data-forid="${id}">Add filter</button>
          <button type="button" class="btn btn-sm btn-outline-primary" data-action="add-filter-group" data-forid="${id}">Add filter group</button>
          <button type="button" class="btn btn-sm btn-outline-danger" data-action="remove" data-forid="${id}">
            <svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
          </button>
        </div>
        <ul data-forid="${id}"></ul>
      </div></div>`;
  }

  function getAntiForgeryToken() {
    const host = document.getElementById("filter-af-token-host");
    const input = host ? host.querySelector("input[name='__RequestVerificationToken']") : null;
    return input ? input.value : null;
  }

  function safeParseJSON(s) { try { return JSON.parse(s); } catch { return null; } }
  function escapeHtml(s){ return String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
  function cssEscape(s){ return CSS && CSS.escape ? CSS.escape(s) : s.replace(/"/g,'\\"'); }
})();
```

**What to keep an eye on**

* `ViewsFilterContentTypeField_ViewModel`, `FilterConditionInputDto`, and the `CurrentView.Filter` shape must match your existing application contracts
* If `TemplateId`/IDs are `Guid` in your domain, adjust types accordingly
* Make sure the ES module path matches your bundling setup; if you pipeline assets, import from that output path instead
* If you prefer to keep Stimulus for now, you can swap in your legacy controller with only two changes:

  * Replace `data-controller="shared--filter"` with `id="filter-root"` and keep the value attributes
  * Update `data-shared--filter-updateurl-value` to the `Url.Page(... "Save")` endpoint

**Deliverables**

* `Filter.cshtml` + `Filter.cshtml.cs` Razor Page
* `_FilterSubtree.cshtml` partial colocated
* `filter.js` vanilla ES module (no Stimulus)
* Legacy controller actions removed once verified

Run the page, add a filter group and a few conditions, change operators/values, hit Save, confirm success toast, and reload to verify persistence.
