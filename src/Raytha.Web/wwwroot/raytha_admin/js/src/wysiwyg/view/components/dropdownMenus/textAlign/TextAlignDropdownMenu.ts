import { EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableDropdownMenuViewComponent } from '../base/UpdateableDropdownBaseComponent';
import { ITextAlignState } from './interfaces/ITextAlignState';
import template from './templates/textAlign.html';

export class TextAlignDropdownMenu extends UpdateableDropdownMenuViewComponent<ITextAlignState> {
   constructor(container: HTMLElement, controllerIdentifier: string, dropdownTextId?: string) {
      super(container, controllerIdentifier, 'text-align-param', dropdownTextId);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected getCheckPlace(newValue: ITextAlignState): HTMLElement {
      const align = newValue.isImageNode
         ? newValue.imageAlign
         : newValue.textAlign;

      return this.dropdownMenuItems.get(String(align ?? 'left'))!;
   }

   protected getStateSelector(): EditorStateSelector<ITextAlignState> {
      return (state) => ({
         textAlign: state.textAlign,
         isImageNode: state.nodeType.isImage,
         imageAlign: state.image.align,
      });
   }

   protected updateDropdownText(newValue: ITextAlignState): void {
      const align = newValue.isImageNode
         ? newValue.imageAlign
         : newValue.textAlign;

      const validAlignments = ['left', 'center', 'right', 'justify'];
      if (this.dropdownElementText && align && validAlignments.includes(align)) {
         validAlignments.forEach(validAlign => {
            this.toggleClass(this.dropdownElementText!, `icon-text-align-${validAlign}`, validAlign === align);
         });
      }
   }
}