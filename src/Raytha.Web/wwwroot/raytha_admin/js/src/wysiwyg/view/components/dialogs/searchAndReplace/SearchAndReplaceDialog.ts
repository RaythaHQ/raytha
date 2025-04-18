import { EditorStateSelector } from 'wysiwyg/state/types';
import { UpdateableViewComponent } from '../../base';
import { ISearchAndReplaceDialog } from './interfaces/ISearchAndReplaceDialog';
import { SEARCH_AND_REPLACE_DIALOG_SELECTORS } from './constants/searchAndReplaceSelectors';
import template from './templates/searchAndReplace.html';

export class SearchAndReplaceDialog extends UpdateableViewComponent<'searchResult'> implements ISearchAndReplaceDialog {
   private isVisible: boolean = false;
   private searchInput: HTMLInputElement;
   private replaceInput: HTMLInputElement;
   private searchResultIndexElement: HTMLElement;
   private searchResultTotalElement: HTMLElement;

   constructor(container: HTMLElement, private bar: HTMLElement, controllerIdentifier: string) {
      super(container, controllerIdentifier);
   }

   public toggleDialog(): void {
      this.isVisible = !this.isVisible;
      this.element.hidden = !this.isVisible;
      this.searchInput.value = '';
      this.replaceInput.value = '';

      if (!this.element.hidden) {
         const headerRect = this.bar.getBoundingClientRect();
         this.setStyle('top', `${headerRect.height + 5}px`);
      }
   }

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.element.hidden = true;
      this.searchInput = this.querySelector(SEARCH_AND_REPLACE_DIALOG_SELECTORS.SEARCH_INPUT);
      this.replaceInput = this.querySelector(SEARCH_AND_REPLACE_DIALOG_SELECTORS.REPLACE_INPUT);
      this.searchResultIndexElement = this.querySelector(SEARCH_AND_REPLACE_DIALOG_SELECTORS.SEARCH_RESULT_INDEX);
      this.searchResultTotalElement = this.querySelector(SEARCH_AND_REPLACE_DIALOG_SELECTORS.SEARCH_RESULT_TOTAL);
   }

   protected getStateSelector(): EditorStateSelector<'searchResult'> {
      return (state) => state.searchResult;
   }

   protected onStateChanged(newValue: { total: number; index: number; }): void {
      this.searchResultIndexElement.textContent = newValue.index.toString();
      this.searchResultTotalElement.textContent = newValue.total.toString();
   }
}