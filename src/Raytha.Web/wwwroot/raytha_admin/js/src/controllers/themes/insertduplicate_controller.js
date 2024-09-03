import { Controller } from 'stimulus'

export default class extends Controller {
   static targets = ["title", "developername", "select"];

   change(event) {
      const selectedOption = event.target.selectedOptions[0];

      if (selectedOption.value == '') {
         this.titleTarget.value = ''
         this.developernameTarget.value = ''
         return;
      }

      this.titleTarget.value = `${selectedOption.text} (duplicate)`;
      this.developernameTarget.value = `${selectedOption.getAttribute('data-developerName')}_duplicate`;
   }
}