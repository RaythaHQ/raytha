import { Controller } from 'stimulus'

export default class extends Controller {

   handleChange(event) {
      const selectedValue = event.target.value;
      const lists = document.querySelectorAll('ul[data-controller="shared--reorderlist"]');
      lists.forEach(list => {
         if (list.id == selectedValue) {
            list.hidden = false;
         } else {
            list.hidden = true;
         }
      });
   }
}