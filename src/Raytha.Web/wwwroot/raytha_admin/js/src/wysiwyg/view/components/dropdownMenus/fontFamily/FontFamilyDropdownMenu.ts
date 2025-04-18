import { EditorStateSelector, EditorStateValue } from 'wysiwyg/state/types';
import { UpdateableDropdownMenuViewComponent } from '../base/UpdateableDropdownBaseComponent';
import { FONTS } from './constants/fonts';
import template from './templates/fontFamily.html';

export class FontFamilyDropdownMenu extends UpdateableDropdownMenuViewComponent<'textStyle.fontFamily'> {
   constructor(container: HTMLElement, controllerIdentifier: string, dropdownTextId?: string) {
      super(container, controllerIdentifier, 'font-family-param', dropdownTextId);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected getStateSelector(): EditorStateSelector<'textStyle.fontFamily'> {
      return (state) => state.textStyle.fontFamily;
   }

   protected getCheckPlace(newValue: string | null): HTMLElement | null {
      if (!newValue) return null;

      const allowedFont = this.getAllowedFont(newValue);

      if (!allowedFont) return null;

      return this.dropdownMenuItems.get(allowedFont) as HTMLElement;
   }

   protected updateDropdownText(newValue: EditorStateValue<'textStyle.fontFamily'>): void {
      if (!newValue || !this.dropdownElementText) return;

      const allowedFont = this.getAllowedFont(newValue);

      if (allowedFont) {
         this.dropdownElementText!.textContent = allowedFont;
      }
      else {
         this.dropdownElementText!.textContent = 'Font family';
      }
   }

   private getAllowedFont(newValue: string): string | undefined {
      const primaryFont = newValue.split(',')[0].trim();
      const cleanedFont = primaryFont.replace(/["']/g, '').trim().toLowerCase();

      return FONTS.find(allowedFont => allowedFont.toLowerCase() === cleanedFont);
   }
}