import { Controller } from "stimulus"
import { v4 as uuidv4 } from 'uuid';
import { Notyf } from 'notyf';

export default class extends Controller {
    static targets = ["parentId", "groupOperator", "filterItem", "filterItemSubtree", "addFilterButton", "addFilterGroupButton", "removeButton", "chooseField", "chooseOperator", "filterValue", "filterValueContainer"]
    static values = { updateurl: String, customfields: Array }

    connect() {
        this.filterRowClassName = "parent_li";
        this.developerNames = this.customfieldsValue.map((o) => o.DeveloperName);
    }

    addFilter(event) {
        var newGuid = uuidv4();
        var currentTreeId = event.currentTarget.getAttribute("data-forid");
        let addAnotherFilter = document.createElement('li');
        addAnotherFilter.className = this.filterRowClassName;
        addAnotherFilter.setAttribute("data-shared--filter-target", "filterItem");
        addAnotherFilter.setAttribute("data-id", newGuid);
        addAnotherFilter.setAttribute("data-type", "filter_condition");
        addAnotherFilter.setAttribute("data-parentid", currentTreeId);
        addAnotherFilter.innerHTML = this.getFilterTemplate(newGuid);
        var childTree = this.filterItemSubtreeTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        childTree.appendChild(addAnotherFilter);
    }

    addFilterGroup(event) {
        var newGuid = uuidv4();
        var currentTreeId = event.currentTarget.getAttribute("data-forid");
        let addAnotherFilterGroup = document.createElement('li');
        addAnotherFilterGroup.className = this.filterRowClassName;
        addAnotherFilterGroup.setAttribute("data-shared--filter-target", "filterItem");
        addAnotherFilterGroup.setAttribute("data-id", newGuid);
        addAnotherFilterGroup.setAttribute("data-type", "filter_condition_group");
        addAnotherFilterGroup.setAttribute("data-parentid", currentTreeId);
        addAnotherFilterGroup.innerHTML = this.getFilterGroupTemplate(newGuid);
        var childTree = this.filterItemSubtreeTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        childTree.appendChild(addAnotherFilterGroup);
    }

    remove(event) {
        var currentTreeId = event.currentTarget.getAttribute("data-forid");
        var filterItem = this.filterItemTargets.find(x => x.getAttribute("data-id") === currentTreeId);
        filterItem.remove();
    }

    chooseField(event) {
        var currentTreeId = event.currentTarget.getAttribute("data-forid");
        var chooseFieldTarget = this.chooseFieldTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        var chooseOperatorTarget = this.chooseOperatorTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        var filterValueContainerTarget = this.filterValueContainerTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        filterValueContainerTarget.parentElement.hidden = true;

        var customField = this.customfieldsValue.find((x) => x.DeveloperName == chooseFieldTarget.value);
        if (customField != null) {
            chooseOperatorTarget.innerHTML = this.getOperatorsForFieldType(customField.FieldType);
            chooseOperatorTarget.parentElement.hidden = false;
        } else {
            chooseOperatorTarget.parentElement.hidden = true;
        }
    }

    chooseOperator(event) {
        var currentTreeId = event.currentTarget.getAttribute("data-forid");
        var chooseFieldTarget = this.chooseFieldTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        var customField = this.customfieldsValue.find((x) => x.DeveloperName == chooseFieldTarget.value);
        var chooseOperatorTarget = this.chooseOperatorTargets.find(x => x.getAttribute("data-forid") === currentTreeId);
        var filterValueContainerTarget = this.filterValueContainerTargets.find(x => x.getAttribute("data-forid") === currentTreeId);

        if (customField != null) {
            filterValueContainerTarget.parentElement.hidden = false;
            filterValueContainerTarget.innerHTML = this.getHtmlFieldForFieldType(chooseOperatorTarget.value, currentTreeId, customField);
        } else {
            filterValueContainerTarget.parentElement.hidden = true;
        }
    }

    save(event) {
        event.preventDefault();
        var filterItems = Array();
        this.filterItemTargets.forEach((el) => {
            var filterItem = { Id: null, ParentId: null, Type: '', GroupOperator: '', Field: '', ConditionOperator: '', Value: '' };   

            var id = el.getAttribute("data-id");
            var parentId = el.getAttribute("data-parentid");
            var filterType = el.getAttribute("data-type");

            filterItem['Id'] = id;
            filterItem['ParentId'] = parentId == '' ? null : parentId;
            filterItem['Type'] = filterType;

            if (filterType == "filter_condition_group") {
                var groupOperator = this.groupOperatorTargets.find(x => x.getAttribute("data-forid") === id && x.value == "and");
                filterItem['GroupOperator'] = groupOperator.checked ? 'and' : 'or';
            } else {
                var chooseFieldTarget = this.chooseFieldTargets.find(x => x.getAttribute("data-forid") === id);
                var chooseOperatorTarget = this.chooseOperatorTargets.find(x => x.getAttribute("data-forid") === id);
                var filterValueTarget = this.filterValueTargets.find(x => x.getAttribute("data-forid") === id);
                filterItem['Field'] = chooseFieldTarget.value;
                filterItem['ConditionOperator'] = chooseOperatorTarget.value;
                if (filterValueTarget != null) {
                    filterItem['Value'] = filterValueTarget.value;
                }
            }

            filterItems.push(filterItem);
        });


        var data = new FormData();
        data.append( "json", JSON.stringify( filterItems ) );
        fetch(this.updateurlValue, {
            method: "POST",
            body: data
          })
          .then (res => res.json())
          .then(res => {
            const notyf = new Notyf();
            if (res.success) {
              notyf.success('Filter succesfully updated');
            } else {
              notyf.error(res.error);
            }
        });
    }

    getFilterGroupTemplate(id) {
        let addAnotherFilterGroupTemplate = `
            <div class="card">
                <div class="card-body">
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" name="groupOperator-${id}" id="andChoice-${id}" type="radio" value="and" data-forid="${id}" data-shared--filter-target="groupOperator" checked>
                        <label class="form-check-label" for="andChoice-${id}">AND</label>
                    </div>
                    <div class="form-check form-check-inline">
                        <input class="form-check-input" name="groupOperator-${id}" id="orChoice-${id}" type="radio" value="or" data-forid="${id}" data-shared--filter-target="groupOperator"> 
                        <label class="form-check-label" for="orChoice-${id}">OR</label>
                    </div>
                    <div class="float-end">
                        <button type="button" class="btn btn-sm btn-outline-primary" data-action="shared--filter#addFilter" data-forid="${id}" data-shared--filter-target="addFilterButton">Add filter</button>
                        <button type="button" class="btn btn-sm btn-outline-primary" data-action="shared--filter#addFilterGroup" data-forid="${id}" data-shared--filter-target="addFilterGroupButton">Add filter group</button>
                        <button type="button" class="btn btn-sm btn-outline-danger" data-action="shared--filter#remove" data-forid="${id}" data-shared--filter-target="removeButton"><svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg></button>
                    </div>
                    <ul data-shared--filter-target="filterItemSubtree" data-forid="${id}"></ul>
                </div>
            </div>`;
        return addAnotherFilterGroupTemplate;
    }

    getFilterTemplate(id) {
        let addAnotherFilterTemplate = `
            <div class="card">
            <div class="card-body">
                <form class="row gy-2 gx-3 align-items-center">
                    <div class="col-md-3">
                        <label class="visually-hidden" for="field">Field</label>
                        <select class="form-select" id="field-${id}" data-forid="${id}" data-shared--filter-target="chooseField" data-action="shared--filter#chooseField">
                            ${this.getDefaultChooseFieldOptions()}
                        </select>
                    </div>
                    <div class="col-md-3" hidden>
                        <label class="visually-hidden" for="operator">Operator</label>
                        <select class="form-select" id="operator-${id}" data-forid="${id}" data-shared--filter-target="chooseOperator" data-action="shared--filter#chooseOperator">
                        </select>
                    </div>
                    <div class="col-md-5" hidden>
                        <label class="visually-hidden">Value</label>
                        <div data-shared--filter-target="filterValueContainer" data-forid="${id}"></div>
                    </div>
                    <div class="col-md-1 ms-auto">
                        <button data-forid="${id}" data-shared--filter-target="removeButton" data-action="shared--filter#remove" type="button" class="btn btn-sm btn-outline-danger"><svg class="icon icon-xs mx-2" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg></button>
                    </div>
                </form>
            </div>
            </div>`;
        return addAnotherFilterTemplate;
    }

    getDefaultChooseFieldOptions() {
        var html = `<option value="" selected>Choose field...</option>`;
        this.developerNames.forEach(developerName => {
            html += `<option value="${developerName}">${developerName}</option>`
        });
        return html;
    }

    getOperatorsForFieldType(fieldType)
    {
        var html = `<option value="" selected>Choose operator...</option>`;

        fieldType.SupportedConditionOperators.forEach(element => {
            html += `<option value="${element.DeveloperName}">${element.Label}</option>`;
        });
        
        return html;
    }

    getHtmlFieldForFieldType(operator, id, customField) {
        var html = "";

        var noValueNeeded = ["empty", "notempty", "", null, "true", "false"];
        if (noValueNeeded.includes(operator))
            return html;

        if (customField.FieldType.HasChoices) {
            html = `
                <select class="form-select" id="filter-value-${id}" data-forid="${id}" data-shared--filter-target="filterValue">
                    <option value="" selected>Choose value...</option>    
                    ${customField.Choices.map((o) => `<option value="${o}">${o}</option>`).join('')}
                </select>
                `;
        } else if (customField.FieldType.DeveloperName == "date") {
            html = `
                <input type="date" class="form-control" data-shared--filter-target="filterValue" data-forid="${id}" id="filter-value-${id}" placeholder="Value">
                `;
        } else if (customField.FieldType.DeveloperName == "number") {
            html = `
                <input type="number" class="form-control" data-shared--filter-target="filterValue" data-forid="${id}" id="filter-value-${id}" placeholder="0">
                `;
        } else {
            html = `
                <input type="text" class="form-control" data-shared--filter-target="filterValue" data-forid="${id}" id="filter-value-${id}" placeholder="Value">
                `;
        }

        return html;
    }
}