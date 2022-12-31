import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["smtpOverrideSystem", "smtpHost", "smtpPort", "smtpUsername", "smtpPassword"]

    connect() {
        if (!this.smtpOverrideSystemTarget.checked) {
            this.smtpHostTarget.readOnly = true;
            this.smtpPortTarget.readOnly = true;
            this.smtpUsernameTarget.readOnly = true;
            this.smtpPasswordTarget.readOnly = true;
        }
    }

    toggleDisableFields(event) {
        if (event.currentTarget.checked) {
            this.smtpHostTarget.readOnly = false;
            this.smtpPortTarget.readOnly = false;
            this.smtpUsernameTarget.readOnly = false;
            this.smtpPasswordTarget.readOnly = false;
        }
        else {
            this.smtpHostTarget.readOnly = true;
            this.smtpPortTarget.readOnly = true;
            this.smtpUsernameTarget.readOnly = true;
            this.smtpPasswordTarget.readOnly = true;
        }
    }
}