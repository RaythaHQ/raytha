import { ModalBaseComponent } from '../base';
import { IWordCountModal } from './interfaces/IWordCountModal';
import { WORD_COUNT_MODAL_SELECTORS } from './constants/wordCountModalSelectors';
import template from './templates/wordCountModal.html';

export class WordCountModal extends ModalBaseComponent implements IWordCountModal {
   private documentWords: HTMLTableCellElement;
   private documentCharacters: HTMLTableCellElement;
   private documentCharactersWithoutSpaces: HTMLTableCellElement;
   private selectionWords: HTMLTableCellElement;
   private selectionCharacters: HTMLTableCellElement;
   private selectionCharactersWithoutSpaces: HTMLTableCellElement;

   protected render(): HTMLElement {
      return this.createElementFromTemplate(template);
   }

   protected initialize(): void {
      this.documentWords = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.DOCUMENT_WORDS);
      this.documentCharacters = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.DOCUMENT_CHARACTERS);
      this.documentCharactersWithoutSpaces = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.DOCUMENT_CHARACTERS_WITHOUT_SPACES);
      this.selectionWords = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.SELECTION_WORDS);
      this.selectionCharacters = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.SELECTION_CHARACTERS);
      this.selectionCharactersWithoutSpaces = this.querySelector<HTMLTableCellElement>(WORD_COUNT_MODAL_SELECTORS.SELECTION_CHARACTERS_WITHOUT_SPACES);
   }

   public setWordCount(wordCount: {
      documentWords: number;
      documentCharacters: number;
      documentCharactersWithoutSpaces: number;
      selectionWords: number;
      selectionCharacters: number;
      selectionCharactersWithoutSpaces: number;
   }): void {
      this.documentWords.textContent = String(wordCount.documentWords);
      this.documentCharacters.textContent = String(wordCount.documentCharacters);
      this.documentCharactersWithoutSpaces.textContent = String(wordCount.documentCharactersWithoutSpaces);
      this.selectionWords.textContent = String(wordCount.selectionWords);
      this.selectionCharacters.textContent = String(wordCount.selectionCharacters);
      this.selectionCharactersWithoutSpaces.textContent = String(wordCount.selectionCharactersWithoutSpaces);
   }
}