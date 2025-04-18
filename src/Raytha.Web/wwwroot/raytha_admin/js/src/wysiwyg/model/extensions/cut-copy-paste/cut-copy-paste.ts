import { Extension } from '@tiptap/core';
import { EditorState } from '@tiptap/pm/state';

// @ts-ignore
declare module "@tiptap/core" {
   interface Commands<ReturnType> {
      cutCopyPaste: {
         cutContent: () => ReturnType,
         copy: () => ReturnType,
         paste: () => ReturnType,
      }
   }
}

export const CutCopyPaste = Extension.create({
   name: 'cutCopyPaste',

   addCommands() {
      return {
         cutContent: () => ({ state, dispatch }) => {
            if (!navigator.clipboard?.writeText) {
               console.error('Clipboard API not supported');

               return false;
            }

            const text = getSelectionContent(state);
            if (!text) return false;

            navigator.clipboard.writeText(text);

            if (dispatch)
               dispatch(state.tr.delete(state.selection.from, state.selection.to));

            return true;
         },
         copy: () => ({ state }) => {
            if (!navigator.clipboard?.writeText) {
               console.error('Clipboard API not supported');

               return false;
            }

            const text = getSelectionContent(state);
            if (!text) return false;

            navigator.clipboard.writeText(text);

            return true;
         },
         paste: () => ({ editor }) => {
            if (!navigator.clipboard?.readText) {
               console.error('Clipboard API not supported');

               return false;
            }

            navigator.clipboard.readText().then((text) => {
               if (!text) return;

               editor.chain().focus().insertContent(text, {
                  parseOptions: {
                     preserveWhitespace: true,
                  }
               }).run();
            });

            return true;
         },
      };
   },
})

function getSelectionContent(state: EditorState) {
   const { empty, from, to } = state.selection;

   if (empty) return '';

   return state.doc.textBetween(from, to);
}