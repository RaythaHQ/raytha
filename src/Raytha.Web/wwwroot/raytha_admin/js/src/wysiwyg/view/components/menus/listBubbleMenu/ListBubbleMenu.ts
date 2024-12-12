import { MenuBaseComponent } from '../base/MenuBaseComponent';
import template from './templates/listBubbleMenu.html';

export class ListBubbleMenu extends MenuBaseComponent {
   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }
}