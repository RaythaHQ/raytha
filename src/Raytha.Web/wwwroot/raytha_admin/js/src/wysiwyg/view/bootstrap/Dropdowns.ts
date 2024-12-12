import { Dropdown } from 'bootstrap';

export class Dropdowns {
   public static initializeNestedDropdowns(container: HTMLElement): void {
      const CLASS_NAME = 'has-child-dropdown-show';
      const originalToggle = Dropdown.prototype.toggle;
      Dropdown.prototype.toggle = function () {
         container.querySelectorAll('.' + CLASS_NAME).forEach(element => {
            element.classList.remove(CLASS_NAME);
         });
         // @ts-ignore
         let dd = this._element.closest('.dropdown').parentNode.closest('.dropdown');
         while (dd && dd !== container) {
            dd.classList.add(CLASS_NAME);
            dd = dd.parentNode.closest('.dropdown');
         }

         return originalToggle.call(this);
      };

      container.querySelectorAll('.dropdown').forEach(dropdown => {
         dropdown.addEventListener('hide.bs.dropdown', (event) => {
            if (dropdown.classList.contains(CLASS_NAME)) {
               dropdown.classList.remove(CLASS_NAME);
               event.preventDefault();
            }

            event.stopPropagation();
         });
      });
   }
}