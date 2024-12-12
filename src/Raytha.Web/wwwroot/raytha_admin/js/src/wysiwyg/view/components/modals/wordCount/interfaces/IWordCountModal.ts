import { IModalBaseComponent } from '../../base';

export interface IWordCountModal extends IModalBaseComponent {
   setWordCount(wordCount: {
      documentWords: number,
      documentCharacters: number,
      documentCharactersWithoutSpaces: number,
      selectionWords: number,
      selectionCharacters: number,
      selectionCharactersWithoutSpaces: number,
   }): void;
}