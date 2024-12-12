import { Tooltip } from 'bootstrap';

export class Tooltips {
   public static initializeBootstrapTooltips(container: HTMLElement): void {
      container.querySelectorAll('[data-bs-toggle="tooltip"], [data-tooltip="tooltip"]').forEach((tooltip) => {
         new Tooltip(tooltip);
      });
   }
}