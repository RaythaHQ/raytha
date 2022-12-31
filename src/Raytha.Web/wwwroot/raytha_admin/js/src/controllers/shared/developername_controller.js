import { Controller } from 'stimulus'

export default class extends Controller {
    static targets = ['label', 'developername']

    setDeveloperName(event) {
        this.developernameTarget.value = this.convertToSlug(event.target.value);
    }

    convertToSlug(text) {
        return text
            .toLowerCase()
            .replace(/[^\w ]+/g, '')
            .replace(/ +/g, '_');
    }
}