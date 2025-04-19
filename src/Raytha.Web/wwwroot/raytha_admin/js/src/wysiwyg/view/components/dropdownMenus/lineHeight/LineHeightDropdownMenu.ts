import { EditorStateValue, EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableDropdownMenuViewComponent } from '../base/UpdateableDropdownBaseComponent';
import template from './templates/lineHeight.html';

export class LineHeightDropdownMenu extends UpdateableDropdownMenuViewComponent<'formats.lineHeight'> {
   constructor(container: HTMLElement, controllerIdentifier: string, dropdownTextId?: string) {
      super(container, controllerIdentifier, 'line-height-param', dropdownTextId);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected getCheckPlace(newValue: EditorStateValue<'formats.lineHeight'>): HTMLElement {
      return this.dropdownMenuItems.get(newValue ?? '1.4') as HTMLElement;
   }

   protected getStateSelector(): EditorStateSelector<'formats.lineHeight'> {
      return (state) => state.formats.lineHeight;
   }
}