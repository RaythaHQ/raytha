import { UpdateableBarComponent } from '../base/UpdateableBarComponent';
import { EditorStateSelector } from 'wysiwyg/state/types';
import { IFooterBarState } from './interfaces/IFooterBarState';
import { FOOTERBAR_SELECTORS } from './constants/footerBarSelectors';
import template from './templates/footerBar.html';

export class FooterBar extends UpdateableBarComponent<IFooterBarState> {
   private cursorPositionElement: HTMLElement;
   private wordsCountElement: HTMLElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.cursorPositionElement = this.querySelector(FOOTERBAR_SELECTORS.CURSOR_POSITION_ELEMENT);
      this.wordsCountElement = this.querySelector(FOOTERBAR_SELECTORS.WORDS_COUNT_ELEMENT);
   }

   protected getStateSelector(): EditorStateSelector<IFooterBarState> {
      return (state) => ({
         cursorPosition: state.cursorPosition,
         words: state.words,
      });
   }

   protected onStateChanged(newValue: IFooterBarState): void {
      this.updateTextContent(this.cursorPositionElement, newValue.cursorPosition);
      this.updateTextContent(this.wordsCountElement, `${newValue.words.toString()} words`);
   }
}