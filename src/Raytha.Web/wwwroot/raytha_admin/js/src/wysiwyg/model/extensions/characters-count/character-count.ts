import { Editor } from '@tiptap/core';
import CharacterCount from '@tiptap/extension-character-count';

export interface CharacterCountStorage {
   charactersWithoutLineBreaks: () => number,
   charactersWithoutSpaces: () => number,
   selectionWords: () => number,
   selectionCharacters: () => number,
   selectionCharactersWithoutSpaces: () => number,
}

export const ExtendedCharacterCount = CharacterCount.extend<{}, CharacterCountStorage>({
   addStorage() {
      return {
         ...this.parent?.(),
         charactersWithoutLineBreaks: () => 0,
         charactersWithoutSpaces: () => 0,
         selectionCharacters: () => 0,
         selectionWords: () => 0,
      };
   },

   onBeforeCreate() {
         this.parent?.(),
         this.storage.charactersWithoutLineBreaks = () => {
            const text = getTextWithoutLineBreaks(this.editor);

            return text.length;
         },

         this.storage.charactersWithoutSpaces = () => {
            const text = getTextWithoutLineBreaks(this.editor);

            return text.replace(/\s/g, '').length;
         },

         this.storage.selectionWords = () => {
            const text = getSelectionText(this.editor);

            return text.split(' ').filter(word => word !== '').length;
         },

         this.storage.selectionCharacters = () => {
            const text = getSelectionText(this.editor);

            return text.length;
         },

         this.storage.selectionCharactersWithoutSpaces = () => {
            const text = getSelectionText(this.editor);

            return text.replace(/ /g, '').length;
         }
   },
});

function getSelectionText(editor: Editor) {
   const { from, to } = editor.state.selection;

   if (from === to) return '';

   return editor.state.doc.textBetween(from, to, '', '');
}

function getTextWithoutLineBreaks(editor: Editor) {
   return editor.state.doc.textBetween(0, editor.state.doc.content.size, '', '');
}