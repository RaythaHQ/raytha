import Swal from 'sweetalert2';

export class SwalAlert {
   private static target: HTMLElement;

   private static getTarget() {
      if (!this.target)
         this.target = document.querySelector('.editor-container') ?? document.body;

      return this.target;
   }

   public static showErrorAlert(message: string) {
      Swal.fire({
         title: 'Error',
         icon: 'error',
         text: message,
         showConfirmButton: false,
         showCloseButton: true,
         target: this.getTarget(),
      });
   }
}