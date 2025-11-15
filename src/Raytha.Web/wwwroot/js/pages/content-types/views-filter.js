/**
 * Content Type Views - Filter Page
 * Handles dynamic tree structure for nested filter conditions
 */

import { ready } from '/js/core/events.js';

/**
 * Show toast notification
 * @param {string} message - Message to display
 * @param {boolean} ok - Success or error
 */
function showNotification(message, ok = true) {
  const n = document.createElement('div');
  n.textContent = message;
  Object.assign(n.style, {
    position: 'fixed',
    bottom: '20px',
    right: '20px',
    padding: '10px 16px',
    borderRadius: '6px',
    color: '#fff',
    fontWeight: '500',
    background: ok ? '#28a745' : '#dc3545',
    boxShadow: '0 2px 6px rgba(0,0,0,.2)',
    zIndex: '9999'
  });
  document.body.appendChild(n);
  setTimeout(() => n.remove(), 2500);
}

/**
 * Initialize filter tree
 */
function initFilterTree() {
  const root = document.getElementById("filter-root");
  if (!root) return;

  const updateUrl = root.getAttribute("data-updateurl");
  const customFields = safeParseJSON(root.getAttribute("data-customfields")) || [];
  const devNames = customFields.map(o => o.DeveloperName);

  // Event delegation for button actions
  root.addEventListener("click", (e) => {
    const el = e.target.closest("[data-action]");
    if (!el) return;
    const action = el.getAttribute("data-action");
    const id = el.getAttribute("data-forid");
    if (action === "add-filter") addFilter(id, root, customFields, devNames);
    if (action === "add-filter-group") addFilterGroup(id, root);
    if (action === "remove") removeNode(id, root);
  });

  // Event delegation for field/operator selection
  root.addEventListener("change", (e) => {
    const fieldSel = e.target.closest("[data-role='choose-field']");
    const opSel = e.target.closest("[data-role='choose-operator']");
    if (fieldSel) onChooseField(fieldSel, root, customFields);
    if (opSel) onChooseOperator(opSel, root, customFields);
  });

  // Save button
  const saveBtn = document.getElementById("filter-save");
  if (saveBtn) {
    saveBtn.addEventListener("click", (e) => onSave(e, root, updateUrl));
  }
}

/**
 * Helper to find subtree for given ID
 */
function subtreeFor(id, root) {
  return root.querySelector(`ul[data-forid="${cssEscape(id)}"]`);
}

/**
 * Helper to find row for given ID
 */
function rowFor(id, root) {
  return root.querySelector(`.parent_li[data-id="${cssEscape(id)}"]`);
}

/**
 * Add a new filter condition
 */
function addFilter(parentId, root, customFields, devNames) {
  const id = crypto.randomUUID();
  const li = document.createElement("li");
  li.className = "parent_li";
  li.setAttribute("data-id", id);
  li.setAttribute("data-type", "filter_condition");
  li.setAttribute("data-parentid", parentId);
  li.innerHTML = filterTemplate(id, devNames);
  
  const target = subtreeFor(parentId, root) || rowFor(parentId, root).querySelector("ul[data-forid]");
  if (target) target.appendChild(li);
}

/**
 * Add a new filter group
 */
function addFilterGroup(parentId, root) {
  const id = crypto.randomUUID();
  const li = document.createElement("li");
  li.className = "parent_li";
  li.setAttribute("data-id", id);
  li.setAttribute("data-type", "filter_condition_group");
  li.setAttribute("data-parentid", parentId);
  li.innerHTML = groupTemplate(id);
  
  const target = subtreeFor(parentId, root) || rowFor(parentId, root).querySelector("ul[data-forid]");
  if (target) target.appendChild(li);
}

/**
 * Remove a node from the tree
 */
function removeNode(id, root) {
  const el = rowFor(id, root);
  if (el) el.remove();
}

/**
 * Handle field selection change
 */
function onChooseField(selectEl, root, customFields) {
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

/**
 * Handle operator selection change
 */
function onChooseOperator(opSelect, root, customFields) {
  const id = opSelect.getAttribute("data-forid");
  const fieldSel = root.querySelector(`#field-${cssEscape(id)}`);
  const field = customFields.find(x => x.DeveloperName === (fieldSel ? fieldSel.value : ""));
  const valueContainer = root.querySelector(`[data-role="filter-value-container"][data-forid="${cssEscape(id)}"]`);
  if (!valueContainer || !field) return;

  const op = opSelect.value;
  valueContainer.parentElement.hidden = false;
  valueContainer.innerHTML = htmlFieldFor(op, id, field);
}

/**
 * Save the filter tree
 */
async function onSave(e, root, updateUrl) {
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
  if (token) {
    fd.append("__RequestVerificationToken", token);
  }

  try {
    const res = await fetch(updateUrl, {
      method: "POST",
      body: fd
    });
    const json = await res.json();
    if (res.ok && json && json.success) {
      showNotification("Filter successfully updated ✅", true);
    } else {
      showNotification(json && json.error ? json.error : "Error saving filter ❌", false);
    }
  } catch {
    showNotification("Network error saving filter ❌", false);
  }
}

/**
 * Generate HTML for field selection dropdown
 */
function defaultChooseFieldOptions(devNames) {
  let html = `<option value="" selected>Choose field...</option>`;
  devNames.forEach(n => { html += `<option value="${escapeHtml(n)}">${escapeHtml(n)}</option>`; });
  return html;
}

/**
 * Generate HTML for operator dropdown based on field type
 */
function operatorsForFieldType(fieldType) {
  let html = `<option value="" selected>Choose operator...</option>`;
  (fieldType?.SupportedConditionOperators || []).forEach(op => {
    html += `<option value="${escapeHtml(op.DeveloperName)}">${escapeHtml(op.Label)}</option>`;
  });
  return html;
}

/**
 * Generate HTML for value input based on operator and field type
 */
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

/**
 * Template for a filter condition
 */
function filterTemplate(id, devNames) {
  return `
    <div class="card"><div class="card-body">
      <form class="row gy-2 gx-3 align-items-center">
        <div class="col-md-3">
          <label class="visually-hidden" for="field">Field</label>
          <select class="form-select" id="field-${id}" data-forid="${id}" data-role="choose-field">
            ${defaultChooseFieldOptions(devNames)}
          </select>
        </div>
        <div class="col-md-3" hidden>
          <label class="visually-hidden" for="operator">Operator</label>
          <select class="form-select" id="operator-${id}" data-forid="${id}" data-role="choose-operator"></select>
        </div>
        <div class="col-md-5" hidden>
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

/**
 * Template for a filter group
 */
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

/**
 * Get antiforgery token from hidden form
 */
function getAntiForgeryToken() {
  const host = document.getElementById("filter-af-token-host");
  const input = host ? host.querySelector("input[name='__RequestVerificationToken']") : null;
  return input ? input.value : null;
}

/**
 * Utility functions
 */
function safeParseJSON(s) { try { return JSON.parse(s); } catch { return null; } }
function escapeHtml(s){ return String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
function cssEscape(s){ return CSS && CSS.escape ? CSS.escape(s) : s.replace(/"/g,'\\"'); }

/**
 * Initialize on page load
 */
function init() {
  initFilterTree();
}

ready(init);

