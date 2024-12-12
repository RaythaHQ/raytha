import { MenuBaseComponent } from '../base/MenuBaseComponent';
import template from './templates/tableBubbleMenu.html';

export class TableBubbleMenu extends MenuBaseComponent {
   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }
}