import { UpdateableBarComponent } from '../base/UpdateableBarComponent';
import { EditorStateSelector } from 'wysiwyg/state/types';
import { MENUBAR_SELECTORS } from './constants/menubarSelectors';
import {
   FormatDropdownMenu,
   FontFamilyDropdownMenu,
   FontSizeDropdownMenu,
   TextAlignDropdownMenu,
   LineHeightDropdownMenu,
   TextColorDropdownMenu,
   BackgroundColorDropdownMenu,
   TableDropdownMenu,
   DateTimeDropdownMenu,
} from '../../dropdownMenus';
import template from './templates/menubar.html';

export class Menubar extends UpdateableBarComponent<'invisibleCharacters'> {
   private tableDropdownMenu: TableDropdownMenu;
   private datetimeDropdownMenu: DateTimeDropdownMenu;
   private formatDropdownMenu: FormatDropdownMenu;
   private fontFamilyDropdownMenu: FontFamilyDropdownMenu;
   private fontSizeDropdownMenu: FontSizeDropdownMenu;
   private textAlignDropdownMenu: TextAlignDropdownMenu;
   private lineHeightDropdownMenu: LineHeightDropdownMenu;
   private textColorDropdownMenu: TextColorDropdownMenu;
   private backgroundColorDropdownMenu: BackgroundColorDropdownMenu;
   private invisibleCharactersButton: HTMLElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const tableDropdown = this.querySelector(MENUBAR_SELECTORS.TABLE_DROPDOWN);
      this.tableDropdownMenu = new TableDropdownMenu(tableDropdown, this.controllerIdentifier);

      const datetimeDropdown = this.querySelector(MENUBAR_SELECTORS.DATETIME_DROPDOWN);
      this.datetimeDropdownMenu = new DateTimeDropdownMenu(datetimeDropdown, this.controllerIdentifier);

      const formatDropdown = this.querySelector(MENUBAR_SELECTORS.FORMAT_DROPDOWN);
      this.formatDropdownMenu = new FormatDropdownMenu(formatDropdown, this.controllerIdentifier);

      const fontFamilyDropdown = this.querySelector(MENUBAR_SELECTORS.FONT_FAMILY_DROPDOWN);
      this.fontFamilyDropdownMenu = new FontFamilyDropdownMenu(fontFamilyDropdown, this.controllerIdentifier);

      const fontSizeDropdown = this.querySelector(MENUBAR_SELECTORS.FONT_SIZE_DROPDOWN);
      this.fontSizeDropdownMenu = new FontSizeDropdownMenu(fontSizeDropdown, this.controllerIdentifier);

      const textAlignDropdown = this.querySelector(MENUBAR_SELECTORS.TEXT_ALIGN_DROPDOWN);
      this.textAlignDropdownMenu = new TextAlignDropdownMenu(textAlignDropdown, this.controllerIdentifier);

      const lineHeightDropdown = this.querySelector(MENUBAR_SELECTORS.LINE_HEIGHT_DROPDOWN);
      this.lineHeightDropdownMenu = new LineHeightDropdownMenu(lineHeightDropdown, this.controllerIdentifier);

      const textColorDropdown = this.querySelector(MENUBAR_SELECTORS.TEXT_COLOR_DROPDOWN);
      this.textColorDropdownMenu = new TextColorDropdownMenu(textColorDropdown, this.controllerIdentifier);

      const backgroundColorDropdown = this.querySelector(MENUBAR_SELECTORS.BACKGROUND_COLOR_DROPDOWN);
      this.backgroundColorDropdownMenu = new BackgroundColorDropdownMenu(backgroundColorDropdown, this.controllerIdentifier);

      this.invisibleCharactersButton = this.querySelector(MENUBAR_SELECTORS.INVISIBLE_CHARACTERS_BUTTON);
   }

   protected getStateSelector(): EditorStateSelector<'invisibleCharacters'> {
      return (state) => state.invisibleCharacters;
   }

   protected onStateChanged(newValue: boolean): void {
      this.updateIcon(this.invisibleCharactersButton, newValue);
   }

   public destroy() {
      this.tableDropdownMenu.destroy();
      this.datetimeDropdownMenu.destroy();
      this.formatDropdownMenu.destroy();
      this.fontFamilyDropdownMenu.destroy();
      this.fontSizeDropdownMenu.destroy();
      this.textAlignDropdownMenu.destroy();
      this.lineHeightDropdownMenu.destroy();
      this.textColorDropdownMenu.destroy();
      this.backgroundColorDropdownMenu.destroy();
      super.destroy();
   }
}
