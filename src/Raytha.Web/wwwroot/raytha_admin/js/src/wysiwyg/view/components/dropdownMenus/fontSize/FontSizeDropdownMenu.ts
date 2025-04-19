import { EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableDropdownMenuViewComponent } from '../base/UpdateableDropdownBaseComponent';
import template from './templates/fontSize.html';

export class FontSizeDropdownMenu extends UpdateableDropdownMenuViewComponent<'textStyle.fontSize'> {
   constructor(container: HTMLElement, controllerIdentifier: string, dropdownTextId?: string) {
      super(container, controllerIdentifier, 'font-size-param', dropdownTextId);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected getStateSelector(): EditorStateSelector<'textStyle.fontSize'> {
      return (state) => state.textStyle.fontSize;
   }

   protected getCheckPlace(newValue: string | null): HTMLElement | null {
      if (!newValue) return null;

      return this.dropdownMenuItems.get(newValue) as HTMLElement;
   }
}