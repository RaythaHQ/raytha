import { Controller } from 'stimulus'
import * as bootstrap from 'bootstrap'

export default class extends Controller {
   static targets = ['source', 'toast'];

   copy(event) {
      var toast = new bootstrap.Toast(this.toastTarget)
      navigator.clipboard.writeText(this.sourceTarget.value).then(function() {
         toast.show();
      });
   }
}