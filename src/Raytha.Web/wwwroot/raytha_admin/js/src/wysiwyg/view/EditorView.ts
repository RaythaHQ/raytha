import { IEditorView } from './interfaces/IEditorView';
import { ModalType, ModalTypeMap, DialogType } from './types';
import { EditorElement } from './enums/EditorViewComponents';
import { Dropdowns, Tooltips } from './bootstrap';
import { EditorContainer } from './components/containers/editorContainer/EditorContainer';
import { ModalContainer } from './components/containers/modalContainer/ModalContainer';
import { SearchAndReplaceDialog } from './components/dialogs/searchAndReplace/SearchAndReplaceDialog';
import { RightClickMenu } from './components/menus/rightClickMenu/RightClickMenu';
import { TableBubbleMenu } from './components/menus/tableBubbleMenu/TableBubbleMenu';
import { ListBubbleMenu } from './components/menus/listBubbleMenu/ListBubbleMenu';

export class EditorView implements IEditorView {
   private readonly editorContainer: EditorContainer;
   private readonly modalContainer: ModalContainer;
   private readonly searchAndReplaceDialog: SearchAndReplaceDialog;

   private rightClickMenu: RightClickMenu;
   private tableBubbleMenu: TableBubbleMenu;
   private listBubbleMenu: ListBubbleMenu;

   constructor(private container: HTMLElement, private controllerIdentifier: string) {
      this.editorContainer = new EditorContainer(container, controllerIdentifier);
      this.modalContainer = new ModalContainer(this.editorContainer.getElement(), controllerIdentifier);

      this.searchAndReplaceDialog = new SearchAndReplaceDialog(this.editorContainer.getElement(), this.editorContainer.getCardHeaderElement(), controllerIdentifier);

      Dropdowns.initializeNestedDropdowns(this.container);
      Tooltips.initializeBootstrapTooltips(this.container);
   }

   public getComponent(component: EditorElement): HTMLElement {
      switch (component) {
         case EditorElement.Footer:
            return this.editorContainer.getFooterbarElement();
         case EditorElement.Toolbar:
            return this.editorContainer.getToolbarElement();
         case EditorElement.Menubar:
            return this.editorContainer.getMenubarElement();
         case EditorElement.RightClickMenu:
            if (!this.rightClickMenu) this.rightClickMenu = new RightClickMenu(this.container, this.controllerIdentifier);
            return this.rightClickMenu.getElement();
         case EditorElement.TableBubbleMenu:
            if (!this.tableBubbleMenu) this.tableBubbleMenu = new TableBubbleMenu(this.container, this.controllerIdentifier);
            return this.tableBubbleMenu.getElement();
         case EditorElement.ListBubbleMenu:
            if (!this.listBubbleMenu) this.listBubbleMenu = new ListBubbleMenu(this.container, this.controllerIdentifier);
            return this.listBubbleMenu.getElement();
         default:
            throw new Error(`Component ${component} not found`);
      }
   }

   public getEditorContainerElement(): HTMLElement {
      return this.editorContainer.getElement();
   }

   public getModal<T extends ModalType>(type: T): ModalTypeMap[T] {
      return this.modalContainer.getModal(type);
   }

   public getDialog(type: DialogType) {
      switch (type) {
         case DialogType.SearchAndReplaceDialog:
            return this.searchAndReplaceDialog;
         default:
            throw new Error(`Dialog ${type} not found`);
      }
   }

   public appendEditor(element: Element) {
      const fragment = document.createDocumentFragment();
      fragment.appendChild(element);
      this.editorContainer.getCardBodyElement().replaceChildren(fragment);
   }

   public getTableBubbleMenuElement(): HTMLElement {
      return new TableBubbleMenu(this.container, this.controllerIdentifier).getElement();
   }

   public toggleSearchAndReplace(): void {
      this.searchAndReplaceDialog.toggleDialog();
   }

   public destroy(): void {
      this.editorContainer.destroy();
      this.modalContainer.destroy();
      this.searchAndReplaceDialog.destroy();
      if (this.rightClickMenu)
         this.rightClickMenu.destroy();

      if (this.tableBubbleMenu)
         this.tableBubbleMenu.destroy();

      if (this.listBubbleMenu)
         this.listBubbleMenu.destroy();
   }
}