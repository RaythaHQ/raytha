import { UpdateableViewComponent } from '../../base/UpdateableViewComponent';
import { EditorStateKeys } from 'wysiwyg/state/types';

export abstract class UpdateableBarComponent<T extends EditorStateKeys> extends UpdateableViewComponent<T> {
   protected updateButtonState(button: HTMLElement, isActive: boolean, activeClass: string = 'active'): void {
      this.toggleClass(button, activeClass, isActive);
   }

   protected updateIcon(element: HTMLElement, isActive: boolean): void {
      this.toggleClass(element, 'icon-check', isActive);
      this.toggleClass(element, 'icon-empty', !isActive);
   }

   protected updateTextContent(element: HTMLElement, content: string): void {
      element.textContent = content;
   }
}