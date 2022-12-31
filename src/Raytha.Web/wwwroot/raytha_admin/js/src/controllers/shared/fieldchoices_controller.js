import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["choices", "developername", "choicesSection", "fieldType", "moveUpButton", "moveDownButton"]
    static values = { nextitemnumber: Number }

    connect() {
        this.formRowClassName = "form-row";
        this.formRowFieldClassName = "form-row-field";
        this.addAnotherKeyValueRowClassName = "row " + this.formRowClassName;
        this.visibleKeyValueElementClassName = "visibleKeyValue";
        this.toggleChoicesSection();
    }

    addanother() {
        let addAnotherKeyValueRow = document.createElement('div');
        addAnotherKeyValueRow.className = this.addAnotherKeyValueRowClassName;
        addAnotherKeyValueRow.innerHTML = this.getRowTemplate();
        this.choicesTarget.appendChild(addAnotherKeyValueRow);
        this.nextitemnumberValue++;
        this.updateMoveChoicesButtons();
    }

    moveUp(event) {
        event.currentTarget.parentElement.parentElement.classList.remove("show");
        event.currentTarget.parentElement.parentElement.previousElementSibling.classList.remove("show");
        var clonedNode = event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement.cloneNode(true);
        this.choicesTarget.insertBefore(clonedNode, event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement.previousElementSibling);
        
        let addAnotherKeyValueRow = event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement;
        this.choicesTarget.removeChild(addAnotherKeyValueRow);
        this.updateFormIndexesForElement();
        this.updateMoveChoicesButtons();
    }

    moveDown(event) {
        var clonedNode = event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement.nextElementSibling.cloneNode(true);
        this.choicesTarget.insertBefore(clonedNode, event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement);
        
        let addAnotherKeyValueRow = event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement.nextElementSibling;
        this.choicesTarget.removeChild(addAnotherKeyValueRow);
        this.updateFormIndexesForElement();
        this.updateMoveChoicesButtons();
    }

    insert(event) {
        let addAnotherKeyValueRow = document.createElement('div');
        addAnotherKeyValueRow.className = this.addAnotherKeyValueRowClassName;
        addAnotherKeyValueRow.innerHTML = this.getRowTemplate();
        this.choicesTarget.insertBefore(addAnotherKeyValueRow, event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement);
        this.updateFormIndexesForElement();
        this.nextitemnumberValue++;
        this.updateMoveChoicesButtons();
    }

    remove(event) {
        let addAnotherKeyValueRow = event.currentTarget.parentElement.parentElement.parentElement.parentElement.parentElement;
        this.choicesTarget.removeChild(addAnotherKeyValueRow);
        this.updateFormIndexesForElement();
        this.nextitemnumberValue--;
        this.updateMoveChoicesButtons();
    }

    handleChangeCheckbox(event) {
        if (event.currentTarget.checked) {
            event.currentTarget.value = "true";
        } else {
            event.currentTarget.value = "false";
        }
    }

    updateFormIndexesForElement() {
        let formRows = this.element.getElementsByClassName(this.formRowClassName);
        for (let i = 0; i < formRows.length; i++) {
            let formRowFields = formRows[i].getElementsByClassName(this.formRowFieldClassName);
            [...formRowFields].forEach(function (formRowField) {
                let pattern = /\[([^\]]+)]/g;
                let newName = formRowField.name.replaceAll(pattern, "[" + i.toString() + "]");
                formRowField.name = newName;
                formRowField.setAttribute("data-ordinal", i.toString());
            });
        }
    }

    setDeveloperName(event) {
        let targetOrdinal = parseInt(event.target.getAttribute("data-ordinal"));
        this.developernameTargets[targetOrdinal].value = this.convertToSlug(event.target.value);
    }

    convertToSlug(text) {
        return text
            .toLowerCase()
            .replace(/[^\w ]+/g, '')
            .replace(/ +/g, '_');
    }

    toggleChoicesSection() {
        var value = this.fieldTypeTarget.value;
        if (value === "radio" || value === "dropdown" || value === "multiple_select") {
            this.choicesSectionTarget.hidden = false;
        } else {
            this.choicesSectionTarget.hidden = true;
        }
        this.updateMoveChoicesButtons();
    }

    updateMoveChoicesButtons() {
        for (let i = 0; i < this.moveUpButtonTargets.length; i++) {
            this.moveUpButtonTargets[i].hidden = (i == 0);
        }
        for (let i = 0; i < this.moveDownButtonTargets.length; i++) {
            this.moveDownButtonTargets[i].hidden = (i == (this.moveDownButtonTargets.length - 1));
        }
    }

    getRowTemplate() {
        let addAnotherkeyValueRowTemplate = `
    <div class="col-4">
        <div class="mb-3">
            <input type="text" name="Choices[${this.nextitemnumberValue}].Label" class="form-control ${this.formRowFieldClassName}" data-action="keyup->shared--fieldchoices#setDeveloperName change->shared--fieldchoices#setDeveloperName" data-ordinal="${this.nextitemnumberValue}" required>
        </div>
    </div>
    <div class="col-4">
        <div class="mb-3">
            <input type="text" name="Choices[${this.nextitemnumberValue}].DeveloperName" class="form-control ${this.formRowFieldClassName}" data-shared--fieldchoices-target="developername" required>
        </div>
    </div>
    <div class="col-3">
        <div class="mb-3">
            <div class="form-check">
                <input class="form-check-input ${this.formRowFieldClassName}" type="checkbox" value="false" name="Choices[${this.nextitemnumberValue}].Disabled" data-action="shared--fieldchoices#handleChangeCheckbox">
                <label class="form-check-label">
                Disabled
                </label>
            </div>
        </div>
    </div>
    <div class="col-1">
        <div class="dropdown">
            <a class="dropdown-toggle" data-bs-toggle="dropdown" data-bs-auto-close="true" href="#" role="button" aria-expanded="false"><svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 12h.01M12 12h.01M19 12h.01M6 12a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0zm7 0a1 1 0 11-2 0 1 1 0 012 0z"></path></svg></a>
            <ul class="dropdown-menu" aria-labelledby="navbarDropdown">
                <li><a class="dropdown-item" href="javascript:void(0);" data-action="shared--fieldchoices#insert"><svg class="icon icon-xs" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4" /></svg> Insert</a></li>
                <li data-shared--fieldchoices-target="moveUpButton"><a class="dropdown-item" href="javascript:void(0);" data-action="shared--fieldchoices#moveUp"><svg class="icon icon-xs" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 10l7-7m0 0l7 7m-7-7v18"></path></svg> Move up</a></li>
                <li data-shared--fieldchoices-target="moveDownButton"><a class="dropdown-item" href="javascript:void(0);" data-action="shared--fieldchoices#moveDown"><svg class="icon icon-xs" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3"></path></svg> Move down</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item link-danger" href="javascript:void(0);" data-action="shared--fieldchoices#remove"><svg class="icon icon-sm" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>Remove</a></li>                                  
            </ul>
        </div>
    </div>`;
        return addAnotherkeyValueRowTemplate;
    }
}