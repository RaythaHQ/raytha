import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["isBaseLayout", "isBaseLayoutInstructions", "templateAccessList", "allowAccessForNewContentTypes"]

    connect() {
        if (!this.isBaseLayoutTarget.checked) {
            this.isBaseLayoutInstructionsTarget.hidden = true;
        }
        else {
            this.templateAccessListTarget.hidden = true;
            this.allowAccessForNewContentTypesTarget.hidden = true;
        }
    }

    toggleBaseLayoutInstructions(event) {
        if (event.currentTarget.checked) {
            this.isBaseLayoutInstructionsTarget.hidden = false;
            this.templateAccessListTarget.hidden = true;
            this.allowAccessForNewContentTypesTarget.hidden = true;
            event.currentTarget.value = true;
        }
        else {
            this.isBaseLayoutInstructionsTarget.hidden = true;
            this.templateAccessListTarget.hidden = false;
            this.allowAccessForNewContentTypesTarget.hidden = false;
            event.currentTarget.value = false;
        }
    }
}