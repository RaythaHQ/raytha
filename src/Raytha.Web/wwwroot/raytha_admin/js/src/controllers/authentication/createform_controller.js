import { Controller } from "stimulus"

export default class extends Controller {
    static targets = ["schemeSelector", "jwtSecretKey", "jwtUseHighSecurity", "samlIdpEntityId", "samlCertificate"]

    connect() {
        this.jwtSecretKeyTarget.hidden = true;
        this.jwtUseHighSecurityTarget.hidden = true;
        this.samlIdpEntityIdTarget.hidden = true;
        this.samlCertificateTarget.hidden = true;
    }

    toggle(event) {
        if (event.currentTarget.value == "jwt") {
            this.jwtSecretKeyTarget.hidden = false;
            this.jwtUseHighSecurityTarget.hidden = false;
            this.samlCertificateTarget.hidden = true;
            this.samlIdpEntityIdTarget.hidden = true;
        } else if (event.currentTarget.value == "saml") {
            this.jwtSecretKeyTarget.hidden = true;
            this.jwtUseHighSecurityTarget.hidden = true;
            this.samlCertificateTarget.hidden = false;
            this.samlIdpEntityIdTarget.hidden = false;
        } else {
            this.jwtSecretKeyTarget.hidden = true;
            this.jwtUseHighSecurityTarget.hidden = true;
            this.samlIdpEntityIdTarget.hidden = true;
            this.samlCertificateTarget.hidden = true;
        }
    }
}