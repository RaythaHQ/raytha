/**
 * Field Choices Controller
 * Manages the dynamic choices UI for select-type fields (radio, dropdown, multiple_select)
 * Handles add, insert, remove, move up/down, and reindexing of choice rows
 */

/**
 * FieldChoicesController class
 * Binds to the field type selector and manages choice row operations
 */
export class FieldChoicesController {
  /**
   * @param {Object} options - Configuration options
   * @param {HTMLSelectElement} options.fieldTypeSelect - The field type select element
   * @param {HTMLElement} options.choicesSection - The container div for the choices section
   * @param {HTMLElement} options.choicesList - The div containing choice rows
   * @param {HTMLElement} options.addAnotherButton - The "Add another choice" button
   * @param {boolean} options.enableAutoSlug - Enable auto-slug generation (kept for future use, currently false)
   * @param {Function} options.onSlugify - Callback for slug generation (optional)
   */
  constructor({ fieldTypeSelect, choicesSection, choicesList, addAnotherButton, enableAutoSlug = false, onSlugify = null }) {
    this.fieldTypeSelect = fieldTypeSelect;
    this.choicesSection = choicesSection;
    this.choicesList = choicesList;
    this.addAnotherButton = addAnotherButton;
    this.enableAutoSlug = enableAutoSlug;
    this.onSlugify = onSlugify;

    this.formRowClass = "form-row";
    this.formRowFieldClass = "form-row-field";

    this.bind();
    this.toggleChoicesSection();
    this.updateMoveButtons();
    this.setupFormSubmission();
  }

  /**
   * Bind all event listeners
   */
  bind() {
    // Field type change handler
    if (this.fieldTypeSelect) {
      this.fieldTypeSelect.addEventListener("change", () => {
        this.toggleChoicesSection();
        this.updateMoveButtons();
      });
    }

    // Add another button handler
    if (this.addAnotherButton) {
      this.addAnotherButton.addEventListener("click", (e) => {
        e.preventDefault();
        this.addRowAtEnd();
      });
    }

    // Delegate row action clicks
    this.choicesList.addEventListener("click", (e) => {
      const actionElement = e.target.closest("[data-action]");
      if (!actionElement) return;

      const action = actionElement.getAttribute("data-action");
      if (!action || action === "addAnother") return;

      e.preventDefault();
      const row = e.target.closest("[data-choice-row]");
      if (!row) return;

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

    // Checkbox change handler is no longer needed since we use hidden field pattern
    // The hidden field always sends "false", and the checkbox sends "true" when checked

    // Optional: label to developer name auto-slug
    this.choicesList.addEventListener("input", (e) => {
      if (!this.enableAutoSlug || !this.onSlugify) return;
      if (e.target.matches("[data-choice-label]")) {
        const row = e.target.closest("[data-choice-row]");
        const devNameInput = row.querySelector("[data-choice-developername]");
        if (devNameInput) {
          devNameInput.value = this.onSlugify(e.target.value);
        }
      }
    });
  }

  /**
   * Setup form submission handler to ensure unchecked checkboxes submit false
   */
  setupFormSubmission() {
    const form = this.choicesList.closest('form');
    if (!form) return;

    form.addEventListener('submit', (e) => {
      // For each unchecked checkbox, create a hidden input with value "false"
      const checkboxes = this.choicesList.querySelectorAll('[data-choice-disabled]');
      checkboxes.forEach(checkbox => {
        if (!checkbox.checked) {
          // Create a hidden input with the same name and value "false"
          const hiddenInput = document.createElement('input');
          hiddenInput.type = 'hidden';
          hiddenInput.name = checkbox.name;
          hiddenInput.value = 'false';
          hiddenInput.setAttribute('data-temp-hidden', 'true'); // Mark for cleanup if needed

          // Insert it right after the checkbox
          checkbox.parentElement.appendChild(hiddenInput);
        }
      });
    });
  }

  /**
   * Toggle visibility of choices section based on field type
   */
  toggleChoicesSection() {
    if (!this.fieldTypeSelect || !this.choicesSection) return;

    const fieldType = this.fieldTypeSelect.value;
    const shouldShow = fieldType === "radio" || fieldType === "dropdown" || fieldType === "multiple_select";

    this.choicesSection.style.display = shouldShow ? "" : "none";
  }

  /**
   * Add a new choice row at the end of the list
   */
  addRowAtEnd() {
    const newRow = this.buildRow();
    this.choicesList.appendChild(newRow);
    this.reindex();
    this.updateMoveButtons();
  }

  /**
   * Insert a new choice row before the specified row
   * @param {HTMLElement} row - The row to insert before
   */
  insertRowBefore(row) {
    const newRow = this.buildRow();
    this.choicesList.insertBefore(newRow, row);
    this.reindex();
    this.updateMoveButtons();
  }

  /**
   * Move a choice row up
   * @param {HTMLElement} row - The row to move
   */
  moveUp(row) {
    const prev = row.previousElementSibling;
    if (prev) {
      this.choicesList.insertBefore(row, prev);
      this.reindex();
      this.updateMoveButtons();
    }
  }

  /**
   * Move a choice row down
   * @param {HTMLElement} row - The row to move
   */
  moveDown(row) {
    const next = row.nextElementSibling;
    if (next) {
      this.choicesList.insertBefore(next, row);
      this.reindex();
      this.updateMoveButtons();
    }
  }

  /**
   * Remove a choice row
   * @param {HTMLElement} row - The row to remove
   */
  removeRow(row) {
    this.choicesList.removeChild(row);
    this.reindex();
    this.updateMoveButtons();
  }

  /**
   * Reindex all choice rows to maintain correct model binding names
   * Updates Form.Choices[i].Label, Form.Choices[i].DeveloperName, Form.Choices[i].Disabled
   */
  reindex() {
    const rows = [...this.choicesList.querySelectorAll("[data-choice-row]")];
    rows.forEach((row, i) => {
      const label = row.querySelector("[data-choice-label]");
      const devName = row.querySelector("[data-choice-developername]");
      const disabled = row.querySelector("[data-choice-disabled]");

      if (label) {
        label.name = `Form.Choices[${i}].Label`;
        label.setAttribute("data-ordinal", String(i));
      }
      if (devName) {
        devName.name = `Form.Choices[${i}].DeveloperName`;
      }
      if (disabled) {
        disabled.name = `Form.Choices[${i}].Disabled`;
      }
    });
  }

  /**
   * Update visibility of move up/down buttons
   * First row: hide "Move up"
   * Last row: hide "Move down"
   */
  updateMoveButtons() {
    const rows = [...this.choicesList.querySelectorAll("[data-choice-row]")];
    rows.forEach((row, i) => {
      const moveUpButton = row.querySelector("[data-move-up]");
      const moveDownButton = row.querySelector("[data-move-down]");

      // Hide the parent <li> element for proper dropdown rendering
      if (moveUpButton) {
        const moveUpLi = moveUpButton.closest("li");
        if (moveUpLi) {
          moveUpLi.style.display = i === 0 ? "none" : "";
        }
      }

      if (moveDownButton) {
        const moveDownLi = moveDownButton.closest("li");
        if (moveDownLi) {
          moveDownLi.style.display = i === rows.length - 1 ? "none" : "";
        }
      }
    });
  }

  /**
   * Build a new choice row element
   * @returns {HTMLElement} - The new row element
   */
  buildRow() {
    const wrapper = document.createElement("div");
    wrapper.className = "row mb-2";
    wrapper.setAttribute("data-choice-row", "");

    wrapper.innerHTML = `
      <div class="col-4">
        <input type="text" 
               name="Form.Choices[0].Label" 
               class="form-control" 
               placeholder="Label"
               data-choice-label 
               data-ordinal="0" 
               required>
      </div>
      <div class="col-4">
        <input type="text" 
               name="Form.Choices[0].DeveloperName" 
               class="form-control" 
               placeholder="Developer Name"
               data-choice-developername 
               required>
      </div>
      <div class="col-3">
        <div class="form-check">
          <input class="form-check-input" 
                 type="checkbox" 
                 value="true" 
                 name="Form.Choices[0].Disabled" 
                 data-choice-disabled>
          <label class="form-check-label">Disabled</label>
        </div>
      </div>
      <div class="col-1">
        <div class="dropdown">
          <a class="dropdown-toggle" data-bs-toggle="dropdown" data-bs-auto-close="true" href="#" role="button" aria-expanded="false">
            <svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h.01M12 12h.01M19 12h.01"/>
            </svg>
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

