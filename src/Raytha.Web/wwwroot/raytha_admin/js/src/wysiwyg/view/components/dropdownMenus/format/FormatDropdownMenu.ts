import { EditorStateValue, EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableDropdownMenuViewComponent } from '../base/UpdateableDropdownBaseComponent';
import template from './templates/format.html';

export class FormatDropdownMenu extends UpdateableDropdownMenuViewComponent<'formats'> {
   constructor(container: HTMLElement, controllerIdentifier: string, dropdownTextId?: string) {
      super(container, controllerIdentifier, 'data-item-role', dropdownTextId);
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected getStateSelector(): EditorStateSelector<'formats'> {
      return (state) => state.formats;
   }

   protected getCheckPlace(newValue: EditorStateValue<'formats'>): HTMLElement | null {
      const activeFormat = Object.entries(newValue).find(([_, isActive]) => isActive);
      if (!activeFormat) return this.dropdownMenuItems.get('paragraph')!;

      const [formatName] = activeFormat;

      return this.dropdownMenuItems.get(formatName)!;
   }

   protected updateItems(newValue: EditorStateValue<'formats'>): void {
      if (!newValue) return;

      this.dropdownMenuItems.forEach((checkPlace, format) => {
         const isActive = newValue[format as keyof EditorStateValue<'formats'>] === true;
         this.toggleClass(checkPlace, 'icon-check', isActive);
         this.toggleClass(checkPlace, 'icon-empty', !isActive);
      });
   }

   protected updateDropdownText(newValue: EditorStateValue<'formats'>): void {
      if (!this.dropdownElementText || !newValue) return;

      const activeFormat = Object.entries(newValue).find(([_, isActive]) => isActive);
      if (activeFormat) {
         const [formatName] = activeFormat;
         const displayName = this.formatNameToDisplayName(formatName);
         this.dropdownElementText.textContent = displayName;
      }
   }

   private formatNameToDisplayName(formatName: string): string {
      const formatDisplayNames: Record<string, string> = {
         'paragraph': 'Paragraph',
         'heading1': 'Heading 1',
         'heading2': 'Heading 2',
         'heading3': 'Heading 3',
         'heading4': 'Heading 4',
         'heading5': 'Heading 5',
         'heading6': 'Heading 6',
      };

      return formatDisplayNames[formatName] || formatName;
   }
}