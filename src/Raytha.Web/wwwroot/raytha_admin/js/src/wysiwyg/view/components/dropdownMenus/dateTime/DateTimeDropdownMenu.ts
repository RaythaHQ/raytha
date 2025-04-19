import { ViewComponent } from 'wysiwyg/view/components/base/ViewComponent';
import template from './templates/dateTime.html';

export class DateTimeDropdownMenu extends ViewComponent {
   private currentTimeElement: HTMLElement;
   private currentDateIsoElement: HTMLElement;
   private currentTime12Element: HTMLElement;
   private currentDateUsElement: HTMLElement;

   constructor(container: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);

      this.currentTimeElement = this.querySelector('[data-item-role="currentTime"]');
      this.currentDateIsoElement = this.querySelector('[data-item-role="currentDateIso"]');
      this.currentTime12Element = this.querySelector('[data-item-role="currentTime12"]');
      this.currentDateUsElement = this.querySelector('[data-item-role="currentDateUs"]');
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.bindEvent(this.container, 'show.bs.dropdown', () => {
         const now = new Date();

         this.currentTimeElement.textContent = now.toLocaleTimeString();
         this.currentDateIsoElement.textContent = now.toISOString().split('T')[0];

         this.currentTime12Element.textContent = now.toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit',
            second: '2-digit',
            hour12: true,
         });

         this.currentDateUsElement.textContent = now.toLocaleDateString('en-US', {
            month: '2-digit',
            day: '2-digit',
            year: 'numeric',
         });
      });
   }
}