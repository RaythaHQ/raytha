import { ViewComponent } from '../../base';
import { IEditorContainer } from './interfaces/IEditorContainer';
import { EDITOR_CONTAINER_SELECTORS } from './constants/editorContainerSelectors';
import { FooterBar } from '../../bars/footerbar/FooterBar';
import { Menubar } from '../../bars/menubar/Menubar';
import { Toolbar } from '../../bars/toolbar/Toolbar';
import template from './templates/editorContainer.html';

export class EditorContainer extends ViewComponent implements IEditorContainer {
   private cardHeader: HTMLElement;
   private cardBody: HTMLElement;
   private menubar: Menubar;
   private toolbar: Toolbar;
   private footer: FooterBar;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.cardHeader = this.querySelector(EDITOR_CONTAINER_SELECTORS.CARD_HEADER);
      this.cardBody = this.querySelector(EDITOR_CONTAINER_SELECTORS.CARD_BODY);
      const cardFooter = this.querySelector(EDITOR_CONTAINER_SELECTORS.CARD_FOOTER);

      this.menubar = new Menubar(this.cardHeader, this.controllerIdentifier);
      this.toolbar = new Toolbar(this.cardHeader, this.controllerIdentifier);
      this.footer = new FooterBar(cardFooter, this.controllerIdentifier);
   }

   public getCardHeaderElement(): HTMLElement {
      return this.cardHeader;
   }

   public getCardBodyElement(): HTMLElement {
      return this.cardBody;
   }

   public getMenubarElement(): HTMLElement {
      return this.menubar.getElement();
   }

   public getToolbarElement(): HTMLElement {
      return this.toolbar.getElement();
   }

   public getFooterbarElement(): HTMLElement {
      return this.footer.getElement();
   }

   public destroy(): void {
      this.menubar.destroy();
      this.toolbar.destroy();
      this.footer.destroy();
      super.destroy();
   }
}