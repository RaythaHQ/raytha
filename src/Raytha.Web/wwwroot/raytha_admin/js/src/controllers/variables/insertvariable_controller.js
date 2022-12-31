import { Controller } from "stimulus"
import * as bootstrap from 'bootstrap'

export default class extends Controller {
    static targets = [ "source", "filterable", "copyAllToClipboard", "variableCheckbox", "toast" ]

    connect() {
        var sectionItems = this.filterableTargets.map((el) => el.getAttribute("data-section-item"));
        let uniqueSections = Array.from(new Set(sectionItems));
        this.visibleSections = { };
        uniqueSections.forEach((el, i) => {
            this.visibleSections[uniqueSections[i]] = true;
        });
    }
    
    filter(event) {
        let lowerCaseFilterTerm = this.sourceTarget.value.toLowerCase();
        for (const prop in this.visibleSections) {
            this.visibleSections[prop] = false;
        }
        
        this.filterableTargets.forEach((el, i) => {
          let filterableKey = el.getAttribute("data-filter-key").toLowerCase();
          let notIncluded = !filterableKey.includes( lowerCaseFilterTerm );
          el.classList.toggle("d-none", notIncluded );
          let sectionItem = el.getAttribute("data-section-item");
          if (!notIncluded) {
            this.visibleSections[sectionItem] = true;
          }
        });

        for (const prop in this.visibleSections) {
            const sectionHeaderEl = document.querySelector(`[data-section="${prop}"]`);
            sectionHeaderEl.classList.toggle("d-none", !this.visibleSections[prop] );
        }
    }

    selectVariable(event) {
        var checkedBoxes = this.variableCheckboxTargets.filter(el => el.checked);
        if (checkedBoxes.length > 0) {
            var itemsLabel = checkedBoxes.length > 1 ? 'variables' : 'variable';
            this.copyAllToClipboardTarget.innerText = `Copy ${checkedBoxes.length} ${itemsLabel} to clipboard`;
            this.copyAllToClipboardTarget.classList.remove('d-none');
        } else {
            this.copyAllToClipboardTarget.classList.add('d-none');
        }
    }

    copyAllToClipboard(event) {
        var checkedBoxes = this.variableCheckboxTargets.filter(el => el.checked);
        var textToCopy = "";
        checkedBoxes.forEach(function(element) {
            textToCopy += `{{ ${element.value} }}\n`
        });

        var toast = new bootstrap.Toast(this.toastTarget)
        navigator.clipboard.writeText(textToCopy).then(function() {
            console.log("copied to clipboard");
            toast.show();
        });
    }
}