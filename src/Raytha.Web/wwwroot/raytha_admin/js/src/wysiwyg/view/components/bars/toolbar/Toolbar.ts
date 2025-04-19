import { UpdateableBarComponent } from '../base/UpdateableBarComponent';
import { EditorStateSelector } from 'wysiwyg/state/types';
import { IToolbarState } from './interfaces/IToolbarState';
import { ToolbarResizable } from './utils/ToolbarResizable';
import { ToolbarButtons } from './utils/ToolbarButtons';
import { TOOLBAR_SELECTORS } from './constants/toolbarSelectors';
import { MENUBAR_SELECTORS } from '../menubar';
import {
   FontFamilyDropdownMenu,
   FontSizeDropdownMenu,
   FormatDropdownMenu,
   TextAlignDropdownMenu,
   TableDropdownMenu,
   TextColorDropdownMenu,
   BackgroundColorDropdownMenu,
} from '../../dropdownMenus';
import template from './templates/toolbar.html';

export class Toolbar extends UpdateableBarComponent<IToolbarState> {
   private tableDropdownMenu: TableDropdownMenu;
   private fontFamilyDropdownMenu: FontFamilyDropdownMenu;
   private fontSizeDropdownMenu: FontSizeDropdownMenu;
   private formatDropdownMenu: FormatDropdownMenu;
   private textAlignDropdownMenu: TextAlignDropdownMenu;
   private textColorDropdownMenu: TextColorDropdownMenu;
   private backgroundColorDropdownMenu: BackgroundColorDropdownMenu;
   private toolbarResizable: ToolbarResizable;
   private toolbarButtons: ToolbarButtons;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      const tableDropdown = this.querySelector(MENUBAR_SELECTORS.TABLE_DROPDOWN);
      this.tableDropdownMenu = new TableDropdownMenu(tableDropdown, this.controllerIdentifier);

      const fontFamilyDropdown = this.querySelector(MENUBAR_SELECTORS.FONT_FAMILY_DROPDOWN);
      this.fontFamilyDropdownMenu = new FontFamilyDropdownMenu(fontFamilyDropdown, this.controllerIdentifier, 'fontFamilySpan');

      const fontSizeDropdown = this.querySelector(MENUBAR_SELECTORS.FONT_SIZE_DROPDOWN);
      this.fontSizeDropdownMenu = new FontSizeDropdownMenu(fontSizeDropdown, this.controllerIdentifier, 'fontSizeSpan');

      const formatDropdown = this.querySelector(MENUBAR_SELECTORS.FORMAT_DROPDOWN);
      this.formatDropdownMenu = new FormatDropdownMenu(formatDropdown, this.controllerIdentifier, 'formatSpan');

      const textAlignDropdown = this.querySelector(MENUBAR_SELECTORS.TEXT_ALIGN_DROPDOWN);
      this.textAlignDropdownMenu = new TextAlignDropdownMenu(textAlignDropdown, this.controllerIdentifier, 'textAlignButton');

      const textColorDropdown = this.querySelector(MENUBAR_SELECTORS.TEXT_COLOR_DROPDOWN);
      this.textColorDropdownMenu = new TextColorDropdownMenu(textColorDropdown, this.controllerIdentifier).withQuickActionButton(`#textColorButton`);

      const backgroundColorDropdown = this.querySelector(MENUBAR_SELECTORS.BACKGROUND_COLOR_DROPDOWN);
      this.backgroundColorDropdownMenu = new BackgroundColorDropdownMenu(backgroundColorDropdown, this.controllerIdentifier).withQuickActionButton(`#backgroundColorButton`);

      const toolbarContainer = this.querySelector<HTMLElement>(TOOLBAR_SELECTORS.TOOLBAR_CONTAINER);
      const toolbar = this.querySelector<HTMLElement>(TOOLBAR_SELECTORS.TOOLBAR);
      const hiddenToolbar = this.querySelector<HTMLElement>(TOOLBAR_SELECTORS.HIDDEN_TOOLBAR);
      const showMoreButtonGroup = this.querySelector<HTMLElement>(TOOLBAR_SELECTORS.SHOW_MORE_BUTTON_GROUP);

      this.toolbarResizable = new ToolbarResizable(toolbarContainer, toolbar, hiddenToolbar, showMoreButtonGroup);
      this.toolbarButtons = new ToolbarButtons(this.element);
   }

   protected getStateSelector(): EditorStateSelector<IToolbarState> {
      return (state) => ({
         marks: state.marks,
         invisibleCharacters: state.invisibleCharacters,
         blockquote: state.blockquote,
         codeBlock: state.codeBlock,
         list: state.list,
         nodeType: state.nodeType,
      });
   }

   protected onStateChanged(newValue: IToolbarState): void {
      this.toolbarButtons.updateStates(newValue);
   }

   public destroy() {
      this.tableDropdownMenu.destroy();
      this.fontFamilyDropdownMenu.destroy();
      this.fontSizeDropdownMenu.destroy();
      this.formatDropdownMenu.destroy();
      this.textAlignDropdownMenu.destroy();
      this.textColorDropdownMenu.destroy();
      this.backgroundColorDropdownMenu.destroy();
      this.toolbarResizable.destroy();
      this.toolbarButtons.destroy();
      super.destroy();
   }
}